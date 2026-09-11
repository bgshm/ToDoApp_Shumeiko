import { CdkDragDrop, CdkDropListGroup } from '@angular/cdk/drag-drop';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TaskDto, TaskState } from '../../core/models/task.models';
import { AuthService } from '../../core/services/auth.service';
import { RealtimeService } from '../../core/services/realtime.service';
import { BoardColumnComponent } from './board-column.component';
import { BoardStore } from './board.store';
import { CategoryDialogComponent } from './category-dialog.component';
import { TaskDialogComponent, TaskDialogResult } from './task-dialog.component';

interface TaskDialogState {
  task: TaskDto | null;
  initialState: TaskState;
}

@Component({
  selector: 'app-board',
  imports: [
    CdkDropListGroup,
    FormsModule,
    BoardColumnComponent,
    TaskDialogComponent,
    CategoryDialogComponent
  ],
  providers: [BoardStore],
  templateUrl: './board.component.html',
  styleUrl: './board.component.css'
})
export class BoardComponent implements OnInit {
  protected readonly store = inject(BoardStore);
  protected readonly auth = inject(AuthService);

  private readonly realtime = inject(RealtimeService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly taskDialog = signal<TaskDialogState | null>(null);
  protected readonly categoryDialogOpen = signal(false);
  protected readonly signingOut = signal(false);

  protected readonly columns: readonly { state: TaskState; label: string }[] = [
    { state: 'ToDo', label: 'To Do' },
    { state: 'Doing', label: 'Doing' },
    { state: 'Done', label: 'Done' }
  ];

  ngOnInit(): void {
    this.store.initialise();

    // Opens the live link to this account's other windows. Failure is handled inside the
    // service: the board still works, it just stops updating on its own.
    void this.realtime.start();

    this.destroyRef.onDestroy(() => void this.realtime.stop());
  }

  protected onSearch(term: string): void {
    this.store.setSearch(term);
  }

  protected onCategoryFilter(value: string): void {
    this.store.setCategoryFilter(value.length > 0 ? value : null);
  }

  protected openNewTask(state: TaskState): void {
    this.taskDialog.set({ task: null, initialState: state });
  }

  protected openTask(task: TaskDto): void {
    this.taskDialog.set({ task, initialState: task.state });
  }

  protected onTaskSaved(result: TaskDialogResult): void {
    if (result.id === null) {
      this.store.createTask(result.request);
    } else {
      this.store.updateTask(result.id, result.request);
    }

    this.taskDialog.set(null);
  }

  protected onTaskDeleted(id: string): void {
    this.store.deleteTask(id);
    this.taskDialog.set(null);
  }

  /**
   * CDK reports the drop index against the target list with the card already removed from
   * it, which is exactly what the API's move endpoint expects -- the store only has to add
   * the current page's offset.
   */
  protected onCardDropped(event: CdkDragDrop<TaskState>): void {
    const task = event.item.data as TaskDto;
    const targetState = event.container.data;

    const sameSlot =
      event.previousContainer === event.container && event.previousIndex === event.currentIndex;

    if (sameSlot) {
      return;
    }

    this.store.moveTask(task, targetState, event.currentIndex);
  }

  protected signOut(): void {
    this.signingOut.set(true);

    void this.realtime.stop();

    this.auth.logout().subscribe({
      next: () => void this.router.navigate(['/login']),
      error: () => void this.router.navigate(['/login'])
    });
  }
}
