/* eslint-disable @typescript-eslint/no-explicit-any */
import {
  Component,
  OnInit,
  ViewChildren,
  QueryList,
  ElementRef,
  OnDestroy,
  inject,
  NgZone,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { AdoptersService } from 'shared/services/adopters.service';
import { firstValueFrom } from 'rxjs';
import {
  ResendCodeRequest,
  UsersService,
  VerificationRequest,
} from 'shared/services/users.service';
import {
  ToastService,
  ErrorHandlingService,
} from '@longhl104/pawfect-match-ng';

@Component({
  selector: 'app-code-verification',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './code-verification.html',
  styleUrl: './code-verification.scss',
})
export class CodeVerification implements OnInit, OnDestroy {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private adoptersService = inject(AdoptersService);
  private toastService = inject(ToastService);
  private errorHandlingService = inject(ErrorHandlingService);
  private ngZone = inject(NgZone);
  private usersService = inject(UsersService);
  private resendTimer: any;
  private userType: 'adopter' | 'shelter' = 'adopter';

  @ViewChildren('codeInput') codeInputs!: QueryList<ElementRef>;

  email = signal<string>('');
  code = signal<string[]>(['', '', '', '', '', '']);
  isSubmitting = signal<boolean>(false);
  isResending = signal<boolean>(false);
  canResend = signal<boolean>(false);
  resendCountdown = signal<number>(60);

  ngOnInit(): void {
    // Get email and userType from query parameters
    this.route.queryParams.subscribe((params) => {
      this.email.set(params['email'] || '');
      this.userType = params['userType'] || 'adopter';

      if (!this.email()) {
        this.toastService.error(
          'Invalid verification link. Please try registering again.',
        );
        this.router.navigate(['/auth/choice']);
        return;
      }

      // Validate email format
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(this.email())) {
        this.toastService.error(
          'Invalid email format. Please try registering again.',
        );
        this.router.navigate(['/auth/choice']);
        return;
      }
    });

    this.startResendTimer();
  }

  ngOnDestroy(): void {
    if (this.resendTimer) {
      clearInterval(this.resendTimer);
    }
  }

  onCodeInput(event: any, index: number): void {
    const value = event.target.value;

    // Only allow single digit
    if (value.length > 1) {
      event.target.value = value.slice(-1);
      this.code.update((prev) => {
        const newCode = [...prev];
        newCode[index] = event.target.value;
        return newCode;
      });
    } else {
      this.code.update((prev) => {
        const newCode = [...prev];
        newCode[index] = value;
        return newCode;
      });
    }

    // Move to next input if digit entered
    if (value && index < 5) {
      const nextInput = this.codeInputs.toArray()[index + 1];
      if (nextInput) {
        nextInput.nativeElement.focus();
      }
    }

    // Auto-submit when all 6 digits are entered
    if (this.isCodeCompleted()) {
      this.verifyCode();
    }
  }

  onKeyDown(event: KeyboardEvent, index: number): void {
    // Handle backspace
    if (event.key === 'Backspace' && !this.code()[index] && index > 0) {
      const prevInput = this.codeInputs.toArray()[index - 1];
      if (prevInput) {
        prevInput.nativeElement.focus();
      }
    }

    // Handle paste
    if (event.key === 'v' && (event.ctrlKey || event.metaKey)) {
      setTimeout(() => this.handlePaste(event, index), 0);
    }
  }

  handlePaste(event: any, index: number): void {
    const pastedData = event.target.value;
    if (pastedData.length === 6 && /^\d{6}$/.test(pastedData)) {
      // Valid 6-digit code pasted
      for (let i = 0; i < 6; i++) {
        this.code.update((prev) => {
          const newCode = [...prev];
          newCode[i] = pastedData[i];
          return newCode;
        });

        const input = this.codeInputs.toArray()[i];
        if (input) {
          input.nativeElement.value = pastedData[i];
        }
      }

      // Clear the original input and auto-submit
      event.target.value = pastedData[index];
      this.verifyCode();
    }
  }

  isCodeCompleted = computed((): boolean => {
    return this.code().every((digit) => digit.length === 1);
  });

  getCodeString = computed((): string => {
    return this.code().join('');
  });

  async verifyCode(): Promise<void> {
    if (!this.isCodeCompleted()) {
      this.toastService.error('Please enter all 6 digits');
      return;
    }

    this.isSubmitting.set(true);

    const request: VerificationRequest = {
      email: this.email(),
      code: this.getCodeString(),
      userType: this.userType,
    };

    try {
      const response = await firstValueFrom(
        this.usersService.verifyCode(request),
      );

      if (response.verified) {
        this.toastService.success(
          'Email verified successfully! You can now log in.',
        );

        // Navigate to login page
        this.router.navigate(['/auth/choice'], {
          queryParams: { verified: 'true' },
        });
      } else {
        this.toastService.error('Invalid verification code. Please try again.');
        this.clearCode();
      }
    } catch (error) {
      this.errorHandlingService.handleErrorWithComponent(
        error,
        this,
        'verifyCode',
      );
      this.clearCode();
    } finally {
      this.isSubmitting.set(false);
    }
  }

  async resendCode(): Promise<void> {
    if (!this.canResend() || this.isResending()) {
      return;
    }

    this.isResending.set(true);

    const request: ResendCodeRequest = {
      email: this.email(),
      userType: this.userType,
    };

    try {
      await firstValueFrom(this.usersService.resendVerificationCode(request));
      this.toastService.success('Verification code sent! Check your email.');

      // Reset timer
      this.canResend.set(false);
      this.resendCountdown.set(60);
      this.startResendTimer();

      // Clear current code
      this.clearCode();
    } catch (error) {
      this.errorHandlingService.handleErrorWithComponent(
        error,
        this,
        'resendCode',
      );
    } finally {
      this.isResending.set(false);
    }
  }

  clearCode(): void {
    this.code.set(['', '', '', '', '', '']);
    this.codeInputs.toArray().forEach((input, index) => {
      input.nativeElement.value = '';
      if (index === 0) {
        input.nativeElement.focus();
      }
    });
  }

  private startResendTimer(): void {
    this.ngZone.runOutsideAngular(() => {
      this.resendTimer = setInterval(() => {
        this.resendCountdown.update((count) => count - 1);
        if (this.resendCountdown() <= 0) {
          this.canResend.set(true);
          clearInterval(this.resendTimer);
        }
      }, 1000);
    });
  }

  goBack(): void {
    this.router.navigate(['/auth/choice']);
  }
}
