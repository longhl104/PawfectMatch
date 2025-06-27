import {
  Component,
  OnInit,
  ViewChildren,
  QueryList,
  ElementRef,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import {
  AdoptersService,
  VerificationRequest,
  ResendCodeRequest,
} from 'shared/services/adopters.service';
import { ToastService } from 'shared/services/toast.service';
import { ErrorHandlingService } from 'shared/services/error-handling.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-code-verification',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './code-verification.html',
  styleUrl: './code-verification.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CodeVerification implements OnInit {
  @ViewChildren('codeInput') codeInputs!: QueryList<ElementRef>;

  email: string = '';
  userType: 'adopter' | 'shelter' = 'adopter';
  code: string[] = ['', '', '', '', '', ''];
  isSubmitting: boolean = false;
  isResending: boolean = false;
  canResend: boolean = false;
  resendCountdown: number = 60;
  private resendTimer: any;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private adoptersService: AdoptersService,
    private toastService: ToastService,
    private errorHandlingService: ErrorHandlingService,
  ) {}

  ngOnInit(): void {
    // Get email and userType from query parameters
    this.route.queryParams.subscribe((params) => {
      this.email = params['email'] || '';
      this.userType = params['userType'] || 'adopter';

      if (!this.email) {
        this.toastService.error(
          'Invalid verification link. Please try registering again.',
        );
        this.router.navigate(['/auth/choice']);
        return;
      }

      // Validate email format
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(this.email)) {
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
      this.code[index] = event.target.value;
    } else {
      this.code[index] = value;
    }

    // Move to next input if digit entered
    if (value && index < 5) {
      const nextInput = this.codeInputs.toArray()[index + 1];
      if (nextInput) {
        nextInput.nativeElement.focus();
      }
    }

    // Auto-submit when all 6 digits are entered
    if (this.isCodeComplete()) {
      this.verifyCode();
    }
  }

  onKeyDown(event: KeyboardEvent, index: number): void {
    // Handle backspace
    if (event.key === 'Backspace' && !this.code[index] && index > 0) {
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
        this.code[i] = pastedData[i];
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

  isCodeComplete(): boolean {
    return this.code.every((digit) => digit.length === 1);
  }

  getCodeString(): string {
    return this.code.join('');
  }

  async verifyCode(): Promise<void> {
    if (!this.isCodeComplete()) {
      this.toastService.error('Please enter all 6 digits');
      return;
    }

    this.isSubmitting = true;

    const request: VerificationRequest = {
      email: this.email,
      code: this.getCodeString(),
      userType: this.userType,
    };

    try {
      const response = await firstValueFrom(
        this.adoptersService.verifyCode(request),
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
      this.isSubmitting = false;
    }
  }

  async resendCode(): Promise<void> {
    if (!this.canResend || this.isResending) {
      return;
    }

    this.isResending = true;

    const request: ResendCodeRequest = {
      email: this.email,
      userType: this.userType,
    };

    try {
      await firstValueFrom(
        this.adoptersService.resendVerificationCode(request),
      );
      this.toastService.success('Verification code sent! Check your email.');

      // Reset timer
      this.canResend = false;
      this.resendCountdown = 60;
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
      this.isResending = false;
    }
  }

  clearCode(): void {
    this.code = ['', '', '', '', '', ''];
    this.codeInputs.toArray().forEach((input, index) => {
      input.nativeElement.value = '';
      if (index === 0) {
        input.nativeElement.focus();
      }
    });
  }

  private startResendTimer(): void {
    this.resendTimer = setInterval(() => {
      this.resendCountdown--;
      if (this.resendCountdown <= 0) {
        this.canResend = true;
        clearInterval(this.resendTimer);
      }
    }, 1000);
  }

  goBack(): void {
    this.router.navigate(['/auth/choice']);
  }
}
