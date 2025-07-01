import { Component, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable, Subscription } from 'rxjs';
import { ToastService, Toast } from '@longhl104/pawfect-match-ng';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.scss',
})
export class ToastContainer implements OnDestroy {
  private toastService = inject(ToastService);

  toasts$: Observable<Toast[]>;
  private subscription: Subscription = new Subscription();

  /** Inserted by Angular inject() migration for backwards compatibility */

  constructor() {
    this.toasts$ = this.toastService.toasts$;
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  trackByToastId(index: number, toast: Toast): string {
    return toast.id;
  }

  closeToast(id: string): void {
    this.toastService.removeToast(id);
  }
}
