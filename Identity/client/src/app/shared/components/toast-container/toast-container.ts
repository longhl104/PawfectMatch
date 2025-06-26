import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from 'shared/services/toast.service';
import { Observable, Subscription } from 'rxjs';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.scss',
})
export class ToastContainer implements OnInit, OnDestroy {
  toasts$: Observable<Toast[]>;
  private subscription: Subscription = new Subscription();

  constructor(private toastService: ToastService) {
    this.toasts$ = this.toastService.toasts$;
  }

  ngOnInit(): void {
    // Any initialization logic if needed
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
