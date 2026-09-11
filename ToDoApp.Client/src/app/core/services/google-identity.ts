/**
 * Minimal typings and loader for Google Identity Services.
 *
 * The library is fetched from Google at runtime rather than bundled, which is how Google
 * ships it: it must be able to update the button and the token flow without the app
 * redeploying. Nothing here runs unless the server reports a configured client id.
 */

const GSI_SCRIPT_URL = 'https://accounts.google.com/gsi/client';

export interface GoogleCredentialResponse {
  credential: string;
}

interface GoogleButtonOptions {
  type?: 'standard' | 'icon';
  theme?: 'outline' | 'filled_blue' | 'filled_black';
  size?: 'small' | 'medium' | 'large';
  text?: 'signin_with' | 'signup_with' | 'continue_with';
  shape?: 'rectangular' | 'pill';
  logo_alignment?: 'left' | 'center';
  width?: number;
}

interface GoogleAccountsId {
  initialize(config: {
    client_id: string;
    callback: (response: GoogleCredentialResponse) => void;
    auto_select?: boolean;
    cancel_on_tap_outside?: boolean;
  }): void;
  renderButton(parent: HTMLElement, options: GoogleButtonOptions): void;
  disableAutoSelect(): void;
}

declare global {
  interface Window {
    google?: { accounts: { id: GoogleAccountsId } };
  }
}

let loader: Promise<GoogleAccountsId> | null = null;

/** Loads the GSI script once per page and resolves with its identity API. */
export function loadGoogleIdentity(): Promise<GoogleAccountsId> {
  if (loader !== null) {
    return loader;
  }

  loader = new Promise<GoogleAccountsId>((resolve, reject) => {
    if (window.google?.accounts?.id) {
      resolve(window.google.accounts.id);
      return;
    }

    const existing = document.querySelector<HTMLScriptElement>(`script[src="${GSI_SCRIPT_URL}"]`);
    const script = existing ?? document.createElement('script');

    const onLoad = () => {
      if (window.google?.accounts?.id) {
        resolve(window.google.accounts.id);
      } else {
        reject(new Error('Google Identity Services loaded but exposed no API.'));
      }
    };

    script.addEventListener('load', onLoad);
    script.addEventListener('error', () =>
      reject(new Error('Could not load Google Identity Services.'))
    );

    if (existing === null) {
      script.src = GSI_SCRIPT_URL;
      script.async = true;
      script.defer = true;
      document.head.appendChild(script);
    }
  }).catch((error: unknown) => {
    // Allow a later attempt to retry rather than replaying a cached rejection forever.
    loader = null;
    throw error;
  });

  return loader;
}
