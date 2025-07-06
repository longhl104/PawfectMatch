import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from '@longhl104/pawfect-match-ng';
import { HeaderComponent } from './components/header/header.component';
import { FooterComponent } from './components/footer/footer.component';
import { AuthService } from 'shared/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    ToastContainerComponent,
    HeaderComponent,
    FooterComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected title = 'client';
  private authService = inject(AuthService);

  ngOnInit() {
    // Authentication is now handled by APP_INITIALIZER before app startup
    // This ensures auth status is checked before any components load
    console.log('PawfectMatch Matcher app initialized');

    this.authService.authStatus$.subscribe((status) => {
      if (status.throwError) {
        throw new Error(status.message);
      }
    });
  }
}
