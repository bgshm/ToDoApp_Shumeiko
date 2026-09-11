export type BoardChangeKind =
  | 'TaskCreated'
  | 'TaskUpdated'
  | 'TaskMoved'
  | 'TaskDeleted'
  | 'CategoryCreated'
  | 'CategoryUpdated'
  | 'CategoryDeleted';

/**
 * Pushed by the server when another window belonging to this account changes something.
 * It says what happened, not what the board now looks like: every window has its own
 * search text, filter and page, so each one re-queries with its own view state.
 */
export interface BoardChange {
  kind: BoardChangeKind;
  taskId: string | null;
  categoryId: string | null;
  occurredAt: string;
}

export function affectsCategories(kind: BoardChangeKind): boolean {
  return kind.startsWith('Category');
}
