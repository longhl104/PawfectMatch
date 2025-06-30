import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-choice',
  imports: [RouterModule],
  templateUrl: './choice.html',
  styleUrl: './choice.scss',
})
export class Choice {
  private router = inject(Router);
  private route = inject(ActivatedRoute);

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
