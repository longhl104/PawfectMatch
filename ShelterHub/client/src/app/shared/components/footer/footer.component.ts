import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { environment } from 'environments/environment';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, DividerModule],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
})
export class FooterComponent {
  readonly contactEmail = 'yenngo20199@gmail.com';
  readonly developerName = 'Hai Yen Ngo';
  readonly currentYear = new Date().getFullYear();
  readonly appVersion = environment.version;

  openEmail(subject: string): void {
    const emailSubject = encodeURIComponent(
      `PawfectMatch ShelterHub - ${subject}`,
    );
    const emailBody = encodeURIComponent(`Hello ${this.developerName},

I am contacting you regarding PawfectMatch ShelterHub.

[Please describe your feedback, issue, or question here]

Best regards,
[Your Name]
[Your Shelter Name]`);

    const mailtoLink = `mailto:${this.contactEmail}?subject=${emailSubject}&body=${emailBody}`;
    window.open(mailtoLink);
  }

  openGoFundMe(): void {
    window.open('https://gofund.me/592f8eed', '_blank');
  }
}
