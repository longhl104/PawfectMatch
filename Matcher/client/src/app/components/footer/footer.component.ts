import { Component } from '@angular/core';

import { RouterModule } from '@angular/router';
import { DividerModule } from 'primeng/divider';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { CustomIconComponent } from '@longhl104/pawfect-match-ng';
import { CONTACT_INFO, CONTACT_ACTIONS, type ContactInfo } from '../../shared';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [
    RouterModule,
    DividerModule,
    ButtonModule,
    TooltipModule,
    CustomIconComponent,
  ],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss'],
})
export class FooterComponent {
  readonly contactInfo: ContactInfo = CONTACT_INFO;
  currentYear = new Date().getFullYear();

  openSocialLink(platform: string) {
    switch (platform.toLowerCase()) {
      case 'facebook':
        CONTACT_ACTIONS.openUrl(this.contactInfo.facebook);
        break;
      case 'instagram':
        CONTACT_ACTIONS.openUrl(this.contactInfo.instagram);
        break;
      default:
        console.log(`Opening ${platform} social link`);
    }
  }
}
