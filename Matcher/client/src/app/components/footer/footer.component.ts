import { Component } from '@angular/core';

import { RouterModule } from '@angular/router';
import { DividerModule } from 'primeng/divider';
import { ButtonModule } from 'primeng/button';
import { CustomIconComponent } from '@longhl104/pawfect-match-ng';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterModule, DividerModule, ButtonModule, CustomIconComponent],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss'],
})
export class FooterComponent {
  currentYear = new Date().getFullYear();

  openSocialLink(platform: string) {
    console.log(`Opening ${platform} social link`);
  }
}
