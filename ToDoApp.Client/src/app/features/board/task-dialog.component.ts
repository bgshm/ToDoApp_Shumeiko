import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  input,
  output,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryDto } from '../../core/models/category.models';
import { CreateTaskRequest, TaskDto, TaskState } from '../../core/models/task.models';

export interface TaskDialogResult {
  /** Null when creating. */
  id: string | null;
  request: CreateTaskRequest;
}

@Component({
  selector: 'app-task-dialog',
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './task-dialog.component.html',
  styleUrl: './dialog.css'
})
export class TaskDialogComponent implements OnInit {
  /** The task being edited, or null when creating a new one. */
  readonly task = input.required<TaskDto | null>();

  /** Column the new card should land in, used only when creating. */
  readonly initialState = input.required<TaskState>();

  readonly categories = input.required<CategoryDto[]>();

  readonly save = output<TaskDialogResult>();
  readonly remove = output<string>();
  readonly dismiss = output<void>();

  protected readonly isEditing = computed(() => this.task() !== null);
  protected readonly confirmingDelete = signal(false);
  protected readonly submitted = signal(false);

  protected readonly title = signal('');
  protected readonly description = signal('');
  protected readonly state = signal<TaskState>('ToDo');
  protected readonly categoryId = signal('');
  protected readonly dueDate = signal('');

  protected readonly titleIsValid = computed(() => this.title().trim().length > 0);

  ngOnInit(): void {
    // The dialog is created fresh each time it opens, so seeding the fields once is enough
    // and nothing later overwrites what the user has typed.
    this.seedFromInputs();
  }

  protected onSubmit(): void {
    this.submitted.set(true);

    if (!this.titleIsValid()) {
      return;
    }

    this.save.emit({
      id: this.task()?.id ?? null,
      request: {
        title: this.title().trim(),
        description: this.description().trim().length > 0 ? this.description().trim() : null,
        state: this.state(),
        categoryId: this.categoryId().length > 0 ? this.categoryId() : null,
        dueDate: toIsoDate(this.dueDate())
      }
    });
  }

  protected onDelete(): void {
    const existing = this.task();
    if (existing === null) {
      return;
    }

    if (!this.confirmingDelete()) {
      this.confirmingDelete.set(true);
      return;
    }

    this.remove.emit(existing.id);
  }

  private seedFromInputs(): void {
    const existing = this.task();

    if (existing === null) {
      this.state.set(this.initialState());
      return;
    }

    this.title.set(existing.title);
    this.description.set(existing.description ?? '');
    this.state.set(existing.state);
    this.categoryId.set(existing.categoryId ?? '');
    this.dueDate.set(toDateInputValue(existing.dueDate));
  }
}

/** <input type="date"> works in yyyy-MM-dd; the API works in ISO instants. */
function toDateInputValue(isoDate: string | null): string {
  if (isoDate === null) {
    return '';
  }

  const parsed = new Date(isoDate);
  return Number.isNaN(parsed.getTime()) ? '' : formatLocalDate(parsed);
}

function toIsoDate(value: string): string | null {
  if (value.length === 0) {
    return null;
  }

  const [year, month, day] = value.split('-').map(Number);
  // Local midnight, so a date picked as "the 5th" does not become the 4th in a
  // timezone behind UTC.
  return new Date(year, month - 1, day).toISOString();
}

function formatLocalDate(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
