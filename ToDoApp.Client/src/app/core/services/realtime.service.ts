import { Injectable, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { API_ROUTES, apiUrl } from '../api.config';
import { BoardChange } from '../models/realtime.models';
import { AuthService } from './auth.service';

/** Client method name the server broadcasts on. Must match SignalRBoardNotifier. */
const BOARD_CHANGED = 'boardChanged';

/**
 * Keeps the SignalR connection to the board hub, and turns server broadcasts into a stream.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);

  private connection: HubConnection | null = null;
  private readonly changesSubject = new Subject<BoardChange>();

  private readonly connectionIdSignal = signal<string | null>(null);
  private readonly connectedSignal = signal(false);

  /** Changes made in this account's other windows. */
  readonly changes: Observable<BoardChange> = this.changesSubject.asObservable();

  /**
   * Sent on every mutating request as X-Connection-Id, so the server can skip echoing the
   * change back to the window that made it.
   */
  readonly connectionId = this.connectionIdSignal.asReadonly();

  readonly connected = this.connectedSignal.asReadonly();

  async start(): Promise<void> {
    if (this.connection !== null || this.auth.token === null) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(apiUrl(API_ROUTES.boardHub), {
        // WebSockets cannot send an Authorization header, so the token goes on the query
        // string; the server accepts it there for the hub route only.
        accessTokenFactory: () => this.auth.token ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on(BOARD_CHANGED, (change: BoardChange) => this.changesSubject.next(change));

    connection.onreconnected((connectionId) => {
      // A reconnect issues a new connection id; without refreshing it the server would
      // stop recognising this window and start echoing its own changes back to it.
      this.connectionIdSignal.set(connectionId ?? null);
      this.connectedSignal.set(true);
    });

    connection.onreconnecting(() => this.connectedSignal.set(false));

    connection.onclose(() => {
      this.connectedSignal.set(false);
      this.connectionIdSignal.set(null);
    });

    this.connection = connection;

    try {
      await connection.start();
      this.connectionIdSignal.set(connection.connectionId);
      this.connectedSignal.set(true);
    } catch {
      // Realtime sync is an enhancement; the board still works by re-querying on demand.
      this.connection = null;
      this.connectedSignal.set(false);
    }
  }

  async stop(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.connectionIdSignal.set(null);
    this.connectedSignal.set(false);

    if (connection !== null && connection.state !== HubConnectionState.Disconnected) {
      await connection.stop();
    }
  }
}
