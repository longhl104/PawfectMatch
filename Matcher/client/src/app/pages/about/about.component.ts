import {
  Component,
  OnInit,
  AfterViewInit,
  ElementRef,
  viewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { AvatarModule } from 'primeng/avatar';
import { TimelineModule } from 'primeng/timeline';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

// Register GSAP plugins
gsap.registerPlugin(ScrollTrigger);

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    CardModule,
    TagModule,
    AvatarModule,
    TimelineModule
  ],
  templateUrl: './about.component.html',
  styleUrls: ['./about.component.scss'],
})
export class AboutComponent implements OnInit, AfterViewInit {
  readonly heroSection = viewChild.required<ElementRef>('heroSection');
  readonly missionSection = viewChild.required<ElementRef>('missionSection');
  readonly valuesSection = viewChild.required<ElementRef>('valuesSection');
  readonly teamSection = viewChild.required<ElementRef>('teamSection');

  // Company values
  values = [
    {
      icon: 'pi pi-heart',
      title: 'Compassion First',
      description: 'Every decision we make is guided by compassion for both pets and the people who love them.',
      color: 'text-pink-500'
    },
    {
      icon: 'pi pi-users',
      title: 'Community Focused',
      description: 'We believe in building strong communities where pets and families thrive together.',
      color: 'text-blue-500'
    },
    {
      icon: 'pi pi-cog',
      title: 'Innovation',
      description: 'We leverage technology to solve real-world problems in pet adoption and care.',
      color: 'text-green-500'
    },
    {
      icon: 'pi pi-shield',
      title: 'Transparency',
      description: 'We operate with honesty and openness in all our interactions with shelters and adopters.',
      color: 'text-orange-500'
    }
  ];

  // Timeline events for company history
  timelineEvents = [
    {
      status: 'Founded',
      date: '2024',
      icon: 'pi pi-lightbulb',
      color: '#2196F3',
      title: 'The Idea',
      description: 'PawfectMatch was born from the desire to revolutionize pet adoption through technology.'
    },
    {
      status: 'Development',
      date: '2024',
      icon: 'pi pi-code',
      color: '#673AB7',
      title: 'Building the Platform',
      description: 'Our team worked tirelessly to create an intuitive, powerful matching algorithm.'
    },
    {
      status: 'Launch',
      date: '2025',
      icon: 'pi pi-rocket',
      color: '#FF9800',
      title: 'Public Launch',
      description: 'PawfectMatch officially launched to help shelters and adopters connect.'
    },
    {
      status: 'Growth',
      date: 'Future',
      icon: 'pi pi-chart-line',
      color: '#4CAF50',
      title: 'Expanding Impact',
      description: 'Our mission continues as we help more pets find their perfect homes.'
    }
  ];

  // Team members (you can expand this with real team information)
  teamMembers = [
    {
      name: 'Development Team',
      role: 'Full-Stack Development',
      description: 'Passionate developers dedicated to creating innovative solutions for pet adoption.',
      avatar: 'pi pi-users',
      skills: ['Angular', 'ASP.NET Core', 'AWS', 'Machine Learning']
    },
    {
      name: 'Design Team',
      role: 'User Experience',
      description: 'Creating intuitive and beautiful experiences for both shelters and adopters.',
      avatar: 'pi pi-palette',
      skills: ['UI/UX Design', 'User Research', 'Accessibility', 'Brand Design']
    },
    {
      name: 'Animal Welfare Consultants',
      role: 'Domain Expertise',
      description: 'Working with shelter professionals to ensure our platform meets real-world needs.',
      avatar: 'pi pi-heart',
      skills: ['Animal Care', 'Shelter Operations', 'Adoption Process', 'Pet Behavior']
    }
  ];

  // Statistics
  stats = [
    { value: '1,000+', label: 'Pets Helped', icon: 'pi pi-heart' },
    { value: '50+', label: 'Partner Shelters', icon: 'pi pi-home' },
    { value: '500+', label: 'Happy Families', icon: 'pi pi-users' },
    { value: '24/7', label: 'Platform Availability', icon: 'pi pi-clock' }
  ];

  ngOnInit() {
    console.log('About component initialized');
  }

  ngAfterViewInit() {
    this.initializeAnimations();
  }

  private initializeAnimations() {
    // Hero section entrance animation
    const heroTimeline = gsap.timeline();

    heroTimeline
      .from('.about-hero-title', {
        duration: 1.2,
        y: 50,
        opacity: 0,
        ease: 'power3.out',
      })
      .from(
        '.about-hero-subtitle',
        {
          duration: 1,
          y: 30,
          opacity: 0,
          ease: 'power2.out',
        },
        '-=0.8',
      );

    // Mission section animation
    gsap.from('.mission-content', {
      scrollTrigger: {
        trigger: '.mission-section',
        start: 'top 80%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 1,
      y: 50,
      opacity: 0,
      ease: 'power2.out',
    });

    // Values cards animation
    gsap.from('.value-card', {
      scrollTrigger: {
        trigger: '.values-section',
        start: 'top 80%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 0.8,
      y: 50,
      opacity: 0,
      stagger: 0.2,
      ease: 'back.out(1.7)',
    });

    // Stats animation
    gsap.from('.stat-item', {
      scrollTrigger: {
        trigger: '.stats-section',
        start: 'top 80%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 0.8,
      scale: 0.8,
      opacity: 0,
      stagger: 0.15,
      ease: 'back.out(1.7)',
    });

    // Team section animation
    gsap.from('.team-card', {
      scrollTrigger: {
        trigger: '.team-section',
        start: 'top 80%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 0.8,
      y: 50,
      opacity: 0,
      stagger: 0.2,
      ease: 'power2.out',
    });

    // Timeline animation
    gsap.from('.timeline-item', {
      scrollTrigger: {
        trigger: '.timeline-section',
        start: 'top 80%',
        end: 'bottom 20%',
        toggleActions: 'play none none reverse',
      },
      duration: 0.6,
      x: -50,
      opacity: 0,
      stagger: 0.2,
      ease: 'power2.out',
    });
  }
}
