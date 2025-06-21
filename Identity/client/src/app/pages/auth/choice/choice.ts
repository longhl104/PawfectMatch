import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-choice',
  imports: [],
  templateUrl: './choice.html',
  styleUrl: './choice.scss',
})
export class Choice {
  constructor(private router: Router) {}

  selectAdopter(): void {
    // Navigate to adopter registration page
    // You can pass data through route state or query params
    this.router.navigate(['/register/adopter']);
  }

  selectShelter(): void {
    // Navigate to shelter admin registration page
    this.router.navigate(['/register/shelter']);
  }
}
