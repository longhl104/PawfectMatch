import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-choice',
  imports: [],
  templateUrl: './choice.html',
  styleUrl: './choice.scss',
})
export class Choice {
  constructor(private router: Router, private route: ActivatedRoute) {}

  selectAdopter(): void {
    // Navigate to adopter registration page
    // You can pass data through route state or query params
    this.router.navigate(['adopter', 'register'], {
      relativeTo: this.route.parent,
    });
  }

  selectShelter(): void {
    // Navigate to shelter admin registration page
    this.router.navigate(['/register/shelter']);
  }
}
