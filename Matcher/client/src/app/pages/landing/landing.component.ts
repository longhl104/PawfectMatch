import {
  Component,
  OnInit,
  AfterViewInit,
  OnDestroy,
  inject,
  PLATFORM_ID,
  ElementRef,
  signal,
  computed,
  effect,
  viewChild,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { CarouselModule } from 'primeng/carousel';
import { DividerModule } from 'primeng/divider';
import { AvatarModule } from 'primeng/avatar';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { AuthService } from 'shared/services/auth.service';
import { CustomIconComponent } from '@longhl104/pawfect-match-ng';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    CardModule,
    CarouselModule,
    DividerModule,
    AvatarModule,
    CustomIconComponent,
  ],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss'],
})
export class LandingComponent implements OnInit, AfterViewInit, OnDestroy {
  private router = inject(Router);
  private authService = inject(AuthService);
  private platformId = inject(PLATFORM_ID);

  readonly heroSection = viewChild.required<ElementRef>('heroSection');
  readonly benefitsSection = viewChild.required<ElementRef>('benefitsSection');
  readonly statsSection = viewChild.required<ElementRef>('statsSection');
  readonly testimonialsSection = viewChild.required<ElementRef>(
    'testimonialsSection',
  );
  readonly finalCtaSection = viewChild.required<ElementRef>('finalCtaSection');

  // Convert arrays to signals for better reactivity
  benefits = signal([
    {
      icon: 'pi pi-cog',
      title: 'Smart Matchmaking Algorithm',
      description:
        'Our advanced algorithm ensures perfect matches between pets and families.',
    },
    {
      icon: 'pi pi-clock',
      title: 'Reduce Time to Adoption',
      description:
        'Streamlined process gets pets into loving homes faster than traditional methods.',
    },
    {
      icon: 'pi pi-heart',
      title: 'Built for Shelters & Adopters',
      description:
        'Designed with input from shelter staff and adopters to meet real-world needs.',
    },
    {
      icon: 'pi pi-globe',
      title: 'Designed to Scale Globally',
      description:
        'Built with scalability in mind to serve shelters and adopters worldwide.',
    },
  ]);

  stats = signal([
    {
      number: 340,
      label: '🐾 Dog Breeds Supported',
      description: 'Comprehensive breed database for accurate matching',
      icon: 'pi pi-users',
      color: 'text-primary',
    },
    {
      number: 1,
      label: '⏱ Second Matching',
      description: 'Instant matching with our advanced algorithm',
      icon: 'pi pi-bolt',
      color: 'text-secondary',
    },
    {
      number: 100,
      label: '🧡 Shelters Across Australia',
      description: 'Built for shelters across Australia and beyond',
      icon: 'pi pi-map-marker',
      color: 'text-orange-500',
    },
  ]);

  testimonials = signal([
    {
      quote:
        'PawfectMatch has revolutionized how we connect pets with families. The matching algorithm is incredibly accurate and saves us so much time.',
      name: 'Sarah M.',
      role: 'Shelter Manager',
      avatar: 'images/pexels-babydov-7788649.jpg',
    },
    {
      quote:
        'We found our perfect companion through PawfectMatch. The process was smooth, and the match was spot-on. Highly recommended!',
      name: 'Mike D.',
      role: 'Happy Adopter',
      avatar: 'images/pexels-babydov-7788874.jpg',
    },
    {
      quote:
        'As a volunteer, I love how PawfectMatch makes it easier to help pets find loving homes. The interface is intuitive and powerful.',
      name: 'Emma L.',
      role: 'Shelter Volunteer',
      avatar: 'images/pexels-babydov-7790096.jpg',
    },
  ]);

  // Convert testimonial index to signal
  currentTestimonialIndex = signal(0);

  // Computed property for current testimonial
  currentTestimonial = computed(() => {
    const testimonials = this.testimonials();
    const index = this.currentTestimonialIndex();
    return testimonials[index];
  });

  // Computed property for testimonials count
  testimonialsCount = computed(() => this.testimonials().length);

  private testimonialInterval: NodeJS.Timeout | null = null;
  private gsapInitialized = false;

  constructor() {
    // Use effect to handle authentication state changes
    effect(() => {
      // This will automatically run when auth status changes if using signals in AuthService
      // For now, we'll keep the subscription approach in ngOnInit
    });
  }

  ngOnInit() {
    // Initialize GSAP plugins only when component loads and only in browser
    if (isPlatformBrowser(this.platformId) && !this.gsapInitialized) {
      try {
        gsap.registerPlugin(ScrollTrigger);
        this.gsapInitialized = true;
        console.log('GSAP plugins initialized for landing page');
      } catch (error) {
        console.error('Failed to initialize GSAP plugins:', error);
      }
    }

    // Check if user is already authenticated and redirect if needed
    this.authService.authStatus$.subscribe((status) => {
      if (status.isAuthenticated) {
        this.router.navigate(['/dashboard']);
      }
    });
  }

  ngAfterViewInit() {
    if (isPlatformBrowser(this.platformId)) {
      // Delay to ensure DOM is fully ready and Angular components are rendered
      setTimeout(() => {
        if (this.checkDOMElements()) {
          this.initializeAnimations();
          this.animateCounters();
          this.setupButtonAnimations();
          this.startTestimonialCarousel();
        } else {
          console.warn('Some DOM elements are not ready, animations skipped');
        }
      }, 300);
    }
  }

  private checkDOMElements(): boolean {
    const criticalElements = [
      '.hero-headline',
      '.primary-cta',
      '.floating-cta',
      '.final-cta-bg',
      '.animated-button',
    ];

    return criticalElements.every((selector) => {
      const element = document.querySelector(selector);
      if (!element) {
        console.warn(`Critical element not found: ${selector}`);
        return false;
      }
      return true;
    });
  }

  private initializeAnimations() {
    // Ensure GSAP is initialized before running animations
    if (!this.gsapInitialized) {
      console.warn('GSAP not initialized, skipping animations');
      return;
    }

    // Hero section animations
    const tl = gsap.timeline();

    tl.from('.hero-headline', {
      duration: 1,
      y: 50,
      opacity: 0,
      ease: 'power3.out',
    })
      .from(
        '.hero-subheadline',
        {
          duration: 0.8,
          y: 30,
          opacity: 0,
          ease: 'power3.out',
        },
        '-=0.5',
      )
      .from(
        '.hero-buttons',
        {
          duration: 0.8,
          y: 30,
          opacity: 0,
          ease: 'power3.out',
        },
        '-=0.3',
      );
    // Hero image animation is now handled by onImageLoad() event

    // Benefits section scroll animations
    gsap.fromTo(
      '.benefit-card',
      {
        y: 60,
        opacity: 0,
      },
      {
        y: 0,
        opacity: 1,
        duration: 0.8,
        stagger: 0.2,
        ease: 'power3.out',
        scrollTrigger: {
          trigger: '.benefits-section',
          start: 'top 80%',
          end: 'bottom 20%',
          toggleActions: 'play none none reverse',
        },
      },
    );

    // Stats section animations
    gsap.fromTo(
      '.stat-card',
      {
        scale: 0.8,
        opacity: 0,
      },
      {
        scale: 1,
        opacity: 1,
        duration: 0.8,
        stagger: 0.15,
        ease: 'back.out(1.7)',
        scrollTrigger: {
          trigger: '.stats-section',
          start: 'top 80%',
          end: 'bottom 20%',
          toggleActions: 'play none none reverse',
        },
      },
    );

    // Testimonials section animations
    gsap.fromTo(
      '.testimonial-card',
      {
        y: 50,
        opacity: 0,
      },
      {
        y: 0,
        opacity: 1,
        duration: 0.6,
        stagger: 0.1,
        ease: 'power2.out',
        scrollTrigger: {
          trigger: '.testimonials-section',
          start: 'top 85%',
          end: 'bottom 20%',
          toggleActions: 'play none none reverse',
        },
      },
    );

    // Final CTA section with parallax effect
    const finalCtaBg = document.querySelector('.final-cta-bg');
    if (finalCtaBg) {
      gsap.fromTo(
        '.final-cta-bg',
        {
          backgroundPosition: 'center 20%',
        },
        {
          backgroundPosition: 'center 80%',
          duration: 2,
          ease: 'none',
          scrollTrigger: {
            trigger: '.final-cta',
            start: 'top bottom',
            end: 'bottom top',
            scrub: 1,
          },
        },
      );
    } else {
      console.warn('Final CTA background element not found');
    }

    // Floating CTA button scroll behavior
    const floatingCta = document.querySelector('.floating-cta');
    const primaryCta = document.querySelector('.primary-cta');

    if (floatingCta && primaryCta) {
      ScrollTrigger.create({
        trigger: '.primary-cta',
        start: 'top center',
        end: 'bottom bottom',
        onEnter: () => {
          gsap.to('.floating-cta', {
            opacity: 1,
            y: 0,
            duration: 0.5,
            ease: 'power3.out',
          });
        },
        onLeave: () => {
          gsap.to('.floating-cta', {
            opacity: 0,
            y: 100,
            duration: 0.5,
            ease: 'power3.in',
          });
        },
        onEnterBack: () => {
          gsap.to('.floating-cta', {
            opacity: 1,
            y: 0,
            duration: 0.5,
            ease: 'power3.out',
          });
        },
        onLeaveBack: () => {
          gsap.to('.floating-cta', {
            opacity: 0,
            y: 100,
            duration: 0.5,
            ease: 'power3.in',
          });
        },
      });
    } else {
      console.warn('Floating CTA or Primary CTA elements not found:', {
        floatingCta: Boolean(floatingCta),
        primaryCta: Boolean(primaryCta),
      });
    }
  }

  private animateCounters() {
    if (!this.gsapInitialized) {
      console.warn('GSAP not initialized, skipping counter animations');
      return;
    }

    this.stats().forEach((stat, index) => {
      const element = document.querySelector(`.stat-number-${index}`);
      if (element) {
        ScrollTrigger.create({
          trigger: element,
          start: 'top 80%',
          onEnter: () => {
            let currentValue = 0;
            const targetValue = stat.number;
            const counter = { value: 0 };

            gsap.to(counter, {
              value: targetValue,
              duration: 2,
              ease: 'power2.out',
              onUpdate: () => {
                currentValue = Math.round(counter.value);
                if (element) {
                  element.textContent = currentValue.toString();
                }
              },
              onComplete: () => {
                // Add a little bounce effect when counter finishes
                gsap.to(element, {
                  scale: 1.1,
                  duration: 0.3,
                  yoyo: true,
                  repeat: 1,
                  ease: 'power2.inOut',
                });
              },
            });
          },
        });
      } else {
        console.warn(`Element with class stat-number-${index} not found`);
      }
    });
  }

  private setupButtonAnimations() {
    if (!this.gsapInitialized) {
      console.warn('GSAP not initialized, skipping button animations');
      return;
    }

    // CTA button hover animations
    const buttons = document.querySelectorAll('.animated-button');

    if (buttons.length > 0) {
      buttons.forEach((button) => {
        button.addEventListener('mouseenter', () => {
          gsap.to(button, {
            scale: 1.05,
            duration: 0.3,
            ease: 'power2.out',
          });
        });

        button.addEventListener('mouseleave', () => {
          gsap.to(button, {
            scale: 1,
            duration: 0.3,
            ease: 'power2.out',
          });
        });
      });
    } else {
      console.warn('No animated buttons found');
    }

    // Pulse animation for primary CTA
    const ctaPulse = document.querySelector('.cta-pulse');
    if (ctaPulse) {
      gsap.to('.cta-pulse', {
        scale: 1.05,
        duration: 1.5,
        repeat: -1,
        yoyo: true,
        ease: 'power2.inOut',
      });
    } else {
      console.warn('CTA pulse element not found');
    }
  }

  private startTestimonialCarousel() {
    this.testimonialInterval = setInterval(() => {
      this.nextTestimonial();
    }, 5000);
  }

  nextTestimonial() {
    const testimonialsLength = this.testimonialsCount();
    if (testimonialsLength > 1) {
      const currentIndex = this.currentTestimonialIndex();
      this.currentTestimonialIndex.set((currentIndex + 1) % testimonialsLength);

      // Animate testimonial change only if GSAP is initialized
      if (this.gsapInitialized) {
        gsap.fromTo(
          '.active-testimonial',
          { opacity: 0, x: 50 },
          { opacity: 1, x: 0, duration: 0.5, ease: 'power2.out' },
        );
      }
    }
  }

  onRegister() {
    // Redirect to Identity service for registration choice
    const identityUrl = this.authService.getIdentityUrl();
    window.location.href = `${identityUrl}/auth/choice`;
  }

  onLogin() {
    // Redirect to Identity service for login
    this.authService.redirectToLogin();
  }

  scrollToSection(sectionId: string) {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth' });
    }
  }

  trackByIndex(index: number): number {
    return index;
  }

  onImageError(event: Event) {
    const img = event.target as HTMLImageElement;
    img.src = '';
  }

  onImageLoad(event: Event) {
    const img = event.target as HTMLImageElement;
    // Set initial state and animate to final state for stable animation only if GSAP is initialized
    if (this.gsapInitialized) {
      gsap.fromTo(
        img,
        {
          opacity: 0,
          scale: 0.8,
        },
        {
          opacity: 1,
          scale: 1,
          duration: 1.5,
          ease: 'power3.out',
          delay: 0.5, // Small delay to sync with other hero animations
        },
      );
    } else {
      // Fallback to simple opacity change if GSAP is not available
      img.style.opacity = '1';
    }
  }

  ngOnDestroy() {
    // Clear testimonial interval
    if (this.testimonialInterval) {
      clearInterval(this.testimonialInterval);
      this.testimonialInterval = null;
    }

    // Clean up GSAP animations and ScrollTriggers
    if (this.gsapInitialized && isPlatformBrowser(this.platformId)) {
      try {
        // Kill all ScrollTriggers created by this component
        ScrollTrigger.getAll().forEach((trigger) => trigger.kill());

        // Kill any ongoing GSAP animations
        gsap.killTweensOf('*');

        console.log('GSAP animations cleaned up for landing page');
      } catch (error) {
        console.error('Error cleaning up GSAP animations:', error);
      }
    }
  }
}
