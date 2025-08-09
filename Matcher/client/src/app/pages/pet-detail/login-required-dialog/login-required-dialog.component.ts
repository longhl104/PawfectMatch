import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { DividerModule } from 'primeng/divider';

@Component({
  selector: 'app-login-required-dialog',
  imports: [
    CommonModule,
    ButtonModule,
    DialogModule,
    DividerModule,
  ],
  templateUrl: './login-required-dialog.component.html',
  styleUrls: ['./login-required-dialog.component.scss']
})
export class LoginRequiredDialogComponent {
  visible = input<boolean>(false);
  petName = input<string>('');

  visibleChange = output<boolean>();
  loginClicked = output<void>();
  signUpClicked = output<void>();

  onHide() {
    this.visibleChange.emit(false);
  }

  onLogin() {
    this.loginClicked.emit();
  }

  onSignUp() {
    this.signUpClicked.emit();
  }
}
