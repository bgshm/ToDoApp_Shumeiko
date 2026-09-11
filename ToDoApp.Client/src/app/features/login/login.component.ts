import { Component, ElementRef, OnInit, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthConfigDto } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import {
  GoogleCredentialResponse,
  loadGoogleIdentity
} from '../../core/services/google-identity';
import { describeHttpError } from '../../core/services/http-error';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  private readonly googleButton = viewChild<ElementRef<HTMLDivElement>>('googleButton');

  protected readonly config = signal<AuthConfigDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly googleError = signal<string | null>(null);

  protected devEmail = 'demo@todoapp.local';
  protected devName = 'Demo user';

  ngOnInit(): void {
    this.auth.loadConfig().subscribe({
      next: (config) => {
        this.config.set(config);
        this.loading.set(false);

        if (config.googleEnabled && config.googleClientId !== null) {
          this.setUpGoogleButton(config.googleClientId);
        }
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(describeHttpError(error, 'Could not reach the server.'));
      }
    });
  }

  protected signInWithDevAccount(): void {
    if (this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.auth.devLogin(this.devEmail.trim(), this.devName.trim()).subscribe({
      next: () => this.goToBoard(),
      error: (error: unknown) => {
        this.submitting.set(false);
        this.error.set(describeHttpError(error, 'Could not sign in.'));
      }
    });
  }

  private setUpGoogleButton(clientId: string): void {
    loadGoogleIdentity()
      .then((identity) => {
        identity.initialize({
          client_id: clientId,
          callback: (response: GoogleCredentialResponse) => this.completeGoogleSignIn(response),
          cancel_on_tap_outside: true
        });

        const host = this.googleButton()?.nativeElement;
        if (host) {
          identity.renderButton(host, {
            type: 'standard',
            theme: 'outline',
            size: 'large',
            text: 'continue_with',
            shape: 'rectangular',
            logo_alignment: 'center',
            width: 320
          });
        }
      })
      .catch(() => {
        this.googleError.set(
          'Google sign-in could not be loaded. Check your connection, or use another method.'
        );
      });
  }

  private completeGoogleSignIn(response: GoogleCredentialResponse): void {
    this.submitting.set(true);
    this.error.set(null);

    this.auth.loginWithGoogle(response.credential).subscribe({
      next: () => this.goToBoard(),
      error: (error: unknown) => {
        this.submitting.set(false);
        this.error.set(describeHttpError(error, 'Google sign-in was rejected.'));
      }
    });
  }

  private goToBoard(): void {
    void this.router.navigate(['/board']);
  }
}
