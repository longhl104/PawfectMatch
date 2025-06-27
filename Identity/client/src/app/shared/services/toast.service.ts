import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  duration?: number;
  closable?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  public toasts$: Observable<Toast[]> = this.toastsSubject.asObservable();

  private defaultDuration = 5000; // 5 seconds

  private generateId(): string {
    return Math.random().toString(36).substring(2) + Date.now().toString(36);
  }

  private addToast(toast: Omit<Toast, 'id'>): void {
    const newToast: Toast = {
      ...toast,
      id: this.generateId(),
      duration: toast.duration ?? this.defaultDuration,
      closable: toast.closable ?? true,
    };

    const currentToasts = this.toastsSubject.value;
    this.toastsSubject.next([...currentToasts, newToast]);

    // Auto-remove toast after duration
    if (newToast.duration && newToast.duration > 0) {
      setTimeout(() => {
        this.removeToast(newToast.id);
      }, newToast.duration);
    }
  }

  success(message: string, duration?: number, closable?: boolean): void {
    this.addToast({
      message,
      type: 'success',
      duration,
      closable,
    });
  }

  error(message: string, duration?: number, closable?: boolean): void {
    this.addToast({
      message,
      type: 'error',
      duration: duration ?? 8000, // Error messages stay longer by default
      closable,
    });
  }

  warning(message: string, duration?: number, closable?: boolean): void {
    this.addToast({
      message,
      type: 'warning',
      duration,
      closable,
    });
  }

  info(message: string, duration?: number, closable?: boolean): void {
    this.addToast({
      message,
      type: 'info',
      duration,
      closable,
    });
  }

  removeToast(id: string): void {
    const currentToasts = this.toastsSubject.value;
    const filteredToasts = currentToasts.filter((toast) => toast.id !== id);
    this.toastsSubject.next(filteredToasts);
  }

  clearAll(): void {
    this.toastsSubject.next([]);
  }
}
