import { Component, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable, Subscription } from 'rxjs';
import { ToastService, Toast } from '../services/toast.service';

@Component({
  selector: 'pm-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-container.component.html',
  styleUrl: './toast-container.component.scss',
})
export class ToastContainerComponent implements OnDestroy {
  private toastService = inject(ToastService);

  toasts$: Observable<Toast[]>;
  private subscription: Subscription = new Subscription();

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
