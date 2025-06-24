import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { RouterModule } from '@angular/router';
import { Location } from '@angular/common';

@Component({
  selector: 'app-privacy',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './privacy.html',
  styleUrl: './privacy.scss',
})
export class Privacy implements OnInit {
  lastUpdated: Date = new Date('2024-06-24');
  showAcceptButton: boolean = false;
  returnUrl: string | null = null;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private location: Location
  ) {}

  ngOnInit() {
    // Check if user came from registration form
    this.route.queryParams.subscribe((params) => {
      this.returnUrl = params['returnUrl'] || null;
      this.showAcceptButton = !!this.returnUrl;
    });
  }

  goBack(): void {
    if (this.returnUrl) {
      this.router.navigate([this.returnUrl]);
    } else {
      this.location.back();
    }
  }

  acceptPrivacy(): void {
    if (this.returnUrl) {
      // Navigate back to registration with acceptance parameter
      this.router.navigate([this.returnUrl], {
        queryParams: { privacyAccepted: 'true' },
      });
    } else {
      this.goBack();
    }
  }
}
