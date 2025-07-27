import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';

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
  contactInfo = {
    email: 'yenngo20199@gmail.com',
    phone: '+61451920109',
    facebook: 'https://www.facebook.com/ngo.hai.yen.932779',
    instagram: 'https://www.instagram.com/n.g.o_haiyen/'
  };

  openEmail() {
    window.open(`mailto:${this.contactInfo.email}`, '_blank');
  }

  callPhone() {
    window.open(`tel:${this.contactInfo.phone}`, '_blank');
  }

  openFacebook() {
    window.open(this.contactInfo.facebook, '_blank', 'noopener,noreferrer');
  }

  openInstagram() {
    window.open(this.contactInfo.instagram, '_blank', 'noopener,noreferrer');
  }
}
