import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { CONTACT_INFO, CONTACT_ACTIONS, type ContactInfo } from '../../shared';

@Component({
  selector: 'app-contact',
  imports: [
    RouterModule,
    ButtonModule,
    CardModule,
    TooltipModule
  ],
  templateUrl: './contact.html',
  styleUrl: './contact.scss',
})
export class ContactComponent {
  readonly contactInfo: ContactInfo = CONTACT_INFO;

  openEmail() {
    CONTACT_ACTIONS.openEmail(this.contactInfo.email);
  }

  callPhone() {
    CONTACT_ACTIONS.callPhone(this.contactInfo.phone);
  }

  openFacebook() {
    CONTACT_ACTIONS.openUrl(this.contactInfo.facebook);
  }

  openInstagram() {
    CONTACT_ACTIONS.openUrl(this.contactInfo.instagram);
  }
}
