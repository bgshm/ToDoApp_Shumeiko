import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TaskDto } from '../../core/models/task.models';

/** How many tilt variants exist in the stylesheet. */
const TILT_VARIANTS = ['a', 'b', 'c', 'd', 'e'] as const;

@Component({
  selector: 'app-task-card',
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './task-card.component.html',
  styleUrl: './task-card.component.css'
})
export class TaskCardComponent {
  readonly task = input.required<TaskDto>();

  readonly open = output<TaskDto>();

  protected readonly stateClass = computed(() => this.task().state.toLowerCase());

  /**
   * Each note sits at a slightly different angle so a column looks stuck on rather than
   * printed. The angle is derived from the id, so it is stable across reloads and drags
   * instead of changing every render.
   */
  protected readonly tiltClass = computed(() => {
    const id = this.task().id;
    let hash = 0;

    for (let index = 0; index < id.length; index++) {
      hash = (hash * 31 + id.charCodeAt(index)) % 9973;
    }

    return `tilt-${TILT_VARIANTS[hash % TILT_VARIANTS.length]}`;
  });

  protected readonly dueState = computed(() => {
    const dueDate = this.task().dueDate;
    if (dueDate === null) {
      return null;
    }

    const due = new Date(dueDate);
    const startOfToday = new Date();
    startOfToday.setHours(0, 0, 0, 0);

    if (due < startOfToday) {
      return 'overdue';
    }

    const startOfTomorrow = new Date(startOfToday);
    startOfTomorrow.setDate(startOfTomorrow.getDate() + 1);

    return due < startOfTomorrow ? 'today' : 'upcoming';
  });
}
