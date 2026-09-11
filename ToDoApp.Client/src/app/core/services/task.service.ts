import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_ROUTES, apiUrl } from '../api.config';
import {
  BoardDto,
  CreateTaskRequest,
  MoveTaskRequest,
  PagedResult,
  TaskDto,
  TaskState,
  UpdateTaskRequest
} from '../models/task.models';

/** Per-column page numbers, plus the filters that apply to the whole board. */
export interface BoardRequest {
  search: string;
  categoryId: string | null;
  pageSize: number;
  toDoPage: number;
  doingPage: number;
  donePage: number;
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  getBoard(request: BoardRequest): Observable<BoardDto> {
    let params = new HttpParams()
      .set('pageSize', request.pageSize)
      .set('toDoPage', request.toDoPage)
      .set('doingPage', request.doingPage)
      .set('donePage', request.donePage);

    if (request.search.trim().length > 0) {
      params = params.set('search', request.search.trim());
    }

    if (request.categoryId !== null) {
      params = params.set('categoryId', request.categoryId);
    }

    return this.http.get<BoardDto>(apiUrl(API_ROUTES.board), { params });
  }

  /** The flat paged list, used by the "all tasks" view rather than the board. */
  getPaged(options: {
    search: string;
    categoryId: string | null;
    state: TaskState | null;
    page: number;
    pageSize: number;
  }): Observable<PagedResult<TaskDto>> {
    let params = new HttpParams().set('page', options.page).set('pageSize', options.pageSize);

    if (options.search.trim().length > 0) {
      params = params.set('search', options.search.trim());
    }

    if (options.categoryId !== null) {
      params = params.set('categoryId', options.categoryId);
    }

    if (options.state !== null) {
      params = params.set('state', options.state);
    }

    return this.http.get<PagedResult<TaskDto>>(apiUrl(API_ROUTES.tasks), { params });
  }

  create(request: CreateTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(apiUrl(API_ROUTES.tasks), request);
  }

  update(id: string, request: UpdateTaskRequest): Observable<TaskDto> {
    return this.http.put<TaskDto>(`${apiUrl(API_ROUTES.tasks)}/${id}`, request);
  }

  move(id: string, request: MoveTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(`${apiUrl(API_ROUTES.tasks)}/${id}/move`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${apiUrl(API_ROUTES.tasks)}/${id}`);
  }
}
