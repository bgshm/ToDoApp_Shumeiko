import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime } from 'rxjs';
import { CategoryDto, SaveCategoryRequest } from '../../core/models/category.models';
import { affectsCategories } from '../../core/models/realtime.models';
import {
  BoardDto,
  CreateTaskRequest,
  PagedResult,
  TaskDto,
  TaskState,
  UpdateTaskRequest,
  emptyBoard
} from '../../core/models/task.models';
import { CategoryService } from '../../core/services/category.service';
import { describeHttpError } from '../../core/services/http-error';
import { RealtimeService } from '../../core/services/realtime.service';
import { TaskService } from '../../core/services/task.service';

const DEFAULT_PAGE_SIZE = 6;
const SEARCH_DEBOUNCE_MS = 250;

/** How long to wait after a broadcast before re-querying, so a burst costs one request. */
const REALTIME_DEBOUNCE_MS = 150;

export type ColumnPages = Record<TaskState, number>;

/**
 * All of the board's view state in one place: filters, per-column page numbers, the loaded
 * cards, and the mutations that change them. Provided by BoardComponent, so the toolbar,
 * the columns and the dialogs all read and write the same instance.
 */
@Injectable()
export class BoardStore {
  private readonly tasks = inject(TaskService);
  private readonly categoriesApi = inject(CategoryService);
  private readonly realtime = inject(RealtimeService);

  private readonly searchInput = new Subject<string>();

  private readonly boardSignal = signal<BoardDto>(emptyBoard(DEFAULT_PAGE_SIZE));
  private readonly categoriesSignal = signal<CategoryDto[]>([]);
  private readonly pagesSignal = signal<ColumnPages>({ ToDo: 1, Doing: 1, Done: 1 });
  private readonly searchSignal = signal('');
  private readonly categoryFilterSignal = signal<string | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);
  private readonly pageSizeSignal = signal(DEFAULT_PAGE_SIZE);

  readonly board = this.boardSignal.asReadonly();
  readonly categories = this.categoriesSignal.asReadonly();
  readonly pages = this.pagesSignal.asReadonly();
  readonly search = this.searchSignal.asReadonly();
  readonly categoryFilter = this.categoryFilterSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly pageSize = this.pageSizeSignal.asReadonly();
  readonly liveSyncActive = this.realtime.connected;

  readonly totalCount = computed(() => {
    const board = this.boardSignal();
    return board.toDo.totalCount + board.doing.totalCount + board.done.totalCount;
  });

  readonly hasFilters = computed(
    () => this.searchSignal().trim().length > 0 || this.categoryFilterSignal() !== null
  );

  constructor() {
    this.searchInput
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchSignal.set(term);
        this.resetPages();
        this.loadBoard();
      });

    // Another window changed something. Which columns and pages that affects depends on
    // this window's own filters, so the only correct response is to re-query.
    this.realtime.changes
      .pipe(debounceTime(REALTIME_DEBOUNCE_MS), takeUntilDestroyed())
      .subscribe((change) => {
        if (affectsCategories(change.kind)) {
          this.loadCategories();
        }

        this.loadBoard();
      });
  }

  column(state: TaskState): PagedResult<TaskDto> {
    const board = this.boardSignal();
    return state === 'ToDo' ? board.toDo : state === 'Doing' ? board.doing : board.done;
  }

  dismissError(): void {
    this.errorSignal.set(null);
  }

  // --- loading -------------------------------------------------------------

  /** Called once when the board mounts. */
  initialise(): void {
    this.loadCategories();
    this.loadBoard();
  }

  loadBoard(): void {
    this.loadingSignal.set(true);

    const pages = this.pagesSignal();

    this.tasks
      .getBoard({
        search: this.searchSignal(),
        categoryId: this.categoryFilterSignal(),
        pageSize: this.pageSizeSignal(),
        toDoPage: pages.ToDo,
        doingPage: pages.Doing,
        donePage: pages.Done
      })
      .subscribe({
        next: (board) => {
          this.boardSignal.set(board);
          this.loadingSignal.set(false);
          this.clampPages(board);
        },
        error: (error: unknown) => {
          this.loadingSignal.set(false);
          this.errorSignal.set(describeHttpError(error, 'Could not load the board.'));
        }
      });
  }

  loadCategories(): void {
    this.categoriesApi.getAll().subscribe({
      next: (categories) => this.categoriesSignal.set(categories),
      error: (error: unknown) =>
        this.errorSignal.set(describeHttpError(error, 'Could not load categories.'))
    });
  }

  /**
   * Emptying the last page of a column -- by deleting, dragging away or filtering -- leaves
   * the view pointing past the end. Step back and reload rather than showing nothing.
   */
  private clampPages(board: BoardDto): void {
    const pages = this.pagesSignal();
    const corrected: ColumnPages = { ...pages };
    let changed = false;

    for (const [state, page] of Object.entries(pages) as [TaskState, number][]) {
      const column = state === 'ToDo' ? board.toDo : state === 'Doing' ? board.doing : board.done;
      const lastPage = Math.max(1, column.totalPages);

      if (page > lastPage) {
        corrected[state] = lastPage;
        changed = true;
      }
    }

    if (changed) {
      this.pagesSignal.set(corrected);
      this.loadBoard();
    }
  }

  // --- filters and paging --------------------------------------------------

  setSearch(term: string): void {
    this.searchInput.next(term);
  }

  setCategoryFilter(categoryId: string | null): void {
    this.categoryFilterSignal.set(categoryId);
    this.resetPages();
    this.loadBoard();
  }

  clearFilters(): void {
    this.searchSignal.set('');
    this.searchInput.next('');
    this.categoryFilterSignal.set(null);
    this.resetPages();
    this.loadBoard();
  }

  goToPage(state: TaskState, page: number): void {
    const column = this.column(state);
    const target = Math.min(Math.max(1, page), Math.max(1, column.totalPages));

    if (target === this.pagesSignal()[state]) {
      return;
    }

    this.pagesSignal.update((pages) => ({ ...pages, [state]: target }));
    this.loadBoard();
  }

  private resetPages(): void {
    this.pagesSignal.set({ ToDo: 1, Doing: 1, Done: 1 });
  }

  // --- task mutations ------------------------------------------------------

  createTask(request: CreateTaskRequest): void {
    this.tasks.create(request).subscribe({
      next: () => {
        // New cards go to the top of their column, so show that column's first page.
        this.pagesSignal.update((pages) => ({ ...pages, [request.state]: 1 }));
        this.loadBoard();
      },
      error: (error: unknown) =>
        this.errorSignal.set(describeHttpError(error, 'Could not create the task.'))
    });
  }

  updateTask(id: string, request: UpdateTaskRequest): void {
    this.tasks.update(id, request).subscribe({
      next: () => this.loadBoard(),
      error: (error: unknown) =>
        this.errorSignal.set(describeHttpError(error, 'Could not save the task.'))
    });
  }

  deleteTask(id: string): void {
    this.tasks.delete(id).subscribe({
      next: () => this.loadBoard(),
      error: (error: unknown) =>
        this.errorSignal.set(describeHttpError(error, 'Could not delete the task.'))
    });
  }

  /**
   * Applies a drop. `indexInPage` is where the card landed within the visible page, which is
   * turned into the absolute position the API expects by adding the page offset.
   *
   * The local board is updated first so the card stays where it was dropped: CDK hands the
   * DOM back to Angular on drop, and without a matching data change the card would visibly
   * snap back to its old slot until the server replied.
   */
  moveTask(task: TaskDto, targetState: TaskState, indexInPage: number): void {
    const pageSize = this.pageSizeSignal();
    const targetPage = this.pagesSignal()[targetState];
    const targetIndex = (targetPage - 1) * pageSize + indexInPage;

    this.applyLocalMove(task, targetState, indexInPage);

    this.tasks.move(task.id, { targetState, targetIndex }).subscribe({
      next: () => this.loadBoard(),
      error: (error: unknown) => {
        this.errorSignal.set(describeHttpError(error, 'Could not move the task.'));
        // The optimistic edit is now a lie; the server's version wins.
        this.loadBoard();
      }
    });
  }

  private applyLocalMove(task: TaskDto, targetState: TaskState, indexInPage: number): void {
    this.boardSignal.update((board) => {
      const columns: Record<TaskState, PagedResult<TaskDto>> = {
        ToDo: { ...board.toDo, items: [...board.toDo.items] },
        Doing: { ...board.doing, items: [...board.doing.items] },
        Done: { ...board.done, items: [...board.done.items] }
      };

      const source = columns[task.state];
      const sourceIndex = source.items.findIndex((item) => item.id === task.id);

      if (sourceIndex !== -1) {
        source.items.splice(sourceIndex, 1);
      }

      const target = columns[targetState];
      const moved: TaskDto = { ...task, state: targetState };
      target.items.splice(Math.min(indexInPage, target.items.length), 0, moved);

      if (task.state !== targetState) {
        // Keep the counters honest until the reload arrives.
        source.totalCount = Math.max(0, source.totalCount - 1);
        target.totalCount += 1;
      }

      return { toDo: columns.ToDo, doing: columns.Doing, done: columns.Done };
    });
  }

  // --- category mutations --------------------------------------------------

  createCategory(request: SaveCategoryRequest, onDone?: (error: string | null) => void): void {
    this.categoriesApi.create(request).subscribe({
      next: () => {
        this.loadCategories();
        onDone?.(null);
      },
      error: (error: unknown) => onDone?.(describeHttpError(error, 'Could not create the category.'))
    });
  }

  updateCategory(
    id: string,
    request: SaveCategoryRequest,
    onDone?: (error: string | null) => void
  ): void {
    this.categoriesApi.update(id, request).subscribe({
      next: () => {
        this.loadCategories();
        // Cards show the category name and colour, so they need redrawing too.
        this.loadBoard();
        onDone?.(null);
      },
      error: (error: unknown) => onDone?.(describeHttpError(error, 'Could not save the category.'))
    });
  }

  deleteCategory(id: string, onDone?: (error: string | null) => void): void {
    this.categoriesApi.delete(id).subscribe({
      next: () => {
        // Filtering by a category that no longer exists would show an empty board.
        if (this.categoryFilterSignal() === id) {
          this.categoryFilterSignal.set(null);
          this.resetPages();
        }

        this.loadCategories();
        this.loadBoard();
        onDone?.(null);
      },
      error: (error: unknown) =>
        onDone?.(describeHttpError(error, 'Could not delete the category.'))
    });
  }
}
