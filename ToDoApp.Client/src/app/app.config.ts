import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { AuthService } from './core/services/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),

    // Turn a token left in storage into a live session before the first route resolves,
    // so a reload lands back on the board instead of bouncing through the sign-in screen.
    // restoreSession() swallows its own failures, so a slow or absent API cannot stop
    // the app from starting.
    provideAppInitializer(() => firstValueFrom(inject(AuthService).restoreSession()))
  ]
};
