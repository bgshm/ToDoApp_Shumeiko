import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { PagedResult, TaskDto, TaskState } from '../../core/models/task.models';
import { PaginatorComponent } from '../../shared/paginator.component';
import { TaskCardComponent } from './task-card.component';

@Component({
  selector: 'app-board-column',
  imports: [CdkDropList, CdkDrag, TaskCardComponent, PaginatorComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './board-column.component.html',
  styleUrl: './board-column.component.css'
})
export class BoardColumnComponent {
  readonly state = input.required<TaskState>();
  readonly label = input.required<string>();
  readonly column = input.required<PagedResult<TaskDto>>();

  /** True while a search or category filter is narrowing the board, for the empty state. */
  readonly filtered = input(false);

  readonly openTask = output<TaskDto>();
  readonly addTask = output<TaskState>();
  readonly pageChange = output<number>();
  readonly cardDropped = output<CdkDragDrop<TaskState>>();

  protected readonly modifier = computed(() => this.state().toLowerCase());
}
