/** The three board columns. Serialised as strings by the API. */
export type TaskState = 'ToDo' | 'Doing' | 'Done';

export const TASK_STATES: readonly TaskState[] = ['ToDo', 'Doing', 'Done'];

export interface TaskDto {
  id: string;
  title: string;
  description: string | null;
  state: TaskState;
  position: number;
  categoryId: string | null;
  categoryName: string | null;
  categoryColor: string | null;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  state: TaskState;
  categoryId: string | null;
  dueDate: string | null;
}

export type UpdateTaskRequest = CreateTaskRequest;

/** Where a dragged card was dropped, in absolute (not per-page) column coordinates. */
export interface MoveTaskRequest {
  targetState: TaskState;
  targetIndex: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface BoardDto {
  toDo: PagedResult<TaskDto>;
  doing: PagedResult<TaskDto>;
  done: PagedResult<TaskDto>;
}

export function emptyPage<T>(pageSize: number): PagedResult<T> {
  return { items: [], page: 1, pageSize, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false };
}

export function emptyBoard(pageSize: number): BoardDto {
  return { toDo: emptyPage(pageSize), doing: emptyPage(pageSize), done: emptyPage(pageSize) };
}
