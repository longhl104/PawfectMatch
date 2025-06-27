import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { CodeVerification } from './code-verification';
import { AdoptersService } from 'shared/services/adopters.service';
import { ToastService } from 'shared/services/toast.service';
import { ErrorHandlingService } from 'shared/services/error-handling.service';

describe('CodeVerification', () => {
  let component: CodeVerification;
  let fixture: ComponentFixture<CodeVerification>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;
  let mockAdoptersService: jasmine.SpyObj<AdoptersService>;
  let mockToastService: jasmine.SpyObj<ToastService>;
  let mockErrorHandlingService: jasmine.SpyObj<ErrorHandlingService>;

  beforeEach(async () => {
    // Create spies
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);
    mockActivatedRoute = {
      queryParams: of({ email: 'test@example.com', userType: 'adopter' })
    };
    mockAdoptersService = jasmine.createSpyObj('AdoptersService', ['verifyCode', 'resendVerificationCode']);
    mockToastService = jasmine.createSpyObj('ToastService', ['success', 'error']);
    mockErrorHandlingService = jasmine.createSpyObj('ErrorHandlingService', ['handleErrorWithComponent']);

    await TestBed.configureTestingModule({
      imports: [CodeVerification],
      providers: [
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: AdoptersService, useValue: mockAdoptersService },
        { provide: ToastService, useValue: mockToastService },
        { provide: ErrorHandlingService, useValue: mockErrorHandlingService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CodeVerification);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with email and userType from query params', () => {
    expect(component.email).toBe('test@example.com');
    expect(component.userType).toBe('adopter');
  });

  it('should handle code input correctly', () => {
    const mockEvent = { target: { value: '1' } };
    component.onCodeInput(mockEvent, 0);

    expect(component.code[0]).toBe('1');
  });

  it('should validate complete code', () => {
    component.code = ['1', '2', '3', '4', '5', '6'];
    expect(component.isCodeComplete()).toBeTruthy();

    component.code = ['1', '2', '3', '', '5', '6'];
    expect(component.isCodeComplete()).toBeFalsy();
  });

  it('should call verification service on valid code submission', async () => {
    component.code = ['1', '2', '3', '4', '5', '6'];
    mockAdoptersService.verifyCode.and.returnValue(of({ message: 'Success', verified: true }));

    await component.verifyCode();

    expect(mockAdoptersService.verifyCode).toHaveBeenCalledWith({
      email: 'test@example.com',
      code: '123456',
      userType: 'adopter'
    });
  });

  it('should show error for invalid code', async () => {
    component.code = ['1', '2', '3', '4', '5', '6'];
    mockAdoptersService.verifyCode.and.returnValue(of({ message: 'Invalid', verified: false }));

    await component.verifyCode();

    expect(mockToastService.error).toHaveBeenCalledWith('Invalid verification code. Please try again.');
  });

  it('should handle resend code functionality', async () => {
    component.canResend = true;
    mockAdoptersService.resendVerificationCode.and.returnValue(of({ message: 'Code sent' }));

    await component.resendCode();

    expect(mockAdoptersService.resendVerificationCode).toHaveBeenCalledWith({
      email: 'test@example.com',
      userType: 'adopter'
    });
    expect(mockToastService.success).toHaveBeenCalledWith('Verification code sent! Check your email.');
  });
});
