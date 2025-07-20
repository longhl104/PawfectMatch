import { Component, input } from '@angular/core';

@Component({
  selector: 'app-logo',
  imports: [],
  templateUrl: './logo.html',
  styleUrl: './logo.scss',
})
export class Logo {
  color = input<string>('var(--p-primary-color)'); // Default color (orange-500)
}
