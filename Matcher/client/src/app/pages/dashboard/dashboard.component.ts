import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService, UserProfile } from '../../shared/services/auth.service';
import { Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ButtonModule, CardModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);

  currentUser$: Observable<UserProfile | null> = this.authService.currentUser$;
  isAuthenticated$ = this.authService.authStatus$;

  ngOnInit() {
    console.log('Dashboard component initialized');
  }

  onLogout() {
    this.authService.logout().subscribe();
  }
}
