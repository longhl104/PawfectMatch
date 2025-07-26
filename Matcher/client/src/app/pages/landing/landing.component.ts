import { Component, OnInit, AfterViewInit, OnDestroy, inject, PLATFORM_ID, ViewChild, ElementRef } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { CarouselModule } from 'primeng/carousel';
import { DividerModule } from 'primeng/divider';
import { AvatarModule } from 'primeng/avatar';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { AuthService } from '../../shared/services/auth.service';

// Register GSAP plugins
if (typeof window !== 'undefined') {
  gsap.registerPlugin(ScrollTrigger);
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    CardModule,
    CarouselModule,
    DividerModule,
    AvatarModule
  ],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent implements OnInit, AfterViewInit, OnDestroy {
  private router = inject(Router);
  private authService = inject(AuthService);
  private platformId = inject(PLATFORM_ID);

  @ViewChild('heroSection', { static: false }) heroSection!: ElementRef;
  @ViewChild('benefitsSection', { static: false }) benefitsSection!: ElementRef;
  @ViewChild('statsSection', { static: false }) statsSection!: ElementRef;
  @ViewChild('testimonialsSection', { static: false }) testimonialsSection!: ElementRef;
  @ViewChild('finalCtaSection', { static: false }) finalCtaSection!: ElementRef;

  benefits = [
    {
      icon: 'pi pi-cog',
      title: 'Smart Matchmaking Algorithm',
      description: 'Our advanced Gale-Shapley algorithm ensures perfect matches between pets and families.'
    },
    {
      icon: 'pi pi-clock',
      title: 'Reduce Time to Adoption',
      description: 'Streamlined process gets pets into loving homes faster than traditional methods.'
    },
    {
      icon: 'pi pi-heart',
      title: 'Built for Shelters & Adopters',
      description: 'Designed with input from shelter staff and adopters to meet real-world needs.'
    },
    {
      icon: 'pi pi-globe',
      title: 'Designed to Scale Globally',
      description: 'Built with scalability in mind to serve shelters and adopters worldwide.'
    }
  ];

  stats = [
    {
      number: 340,
      label: '🐾 Dog Breeds Supported',
      description: 'Comprehensive breed database for accurate matching',
      icon: 'pi pi-users',
      color: 'text-primary'
    },
    {
      number: 1,
      label: '⏱ Second Matching',
      description: 'Instant matching with Gale-Shapley Algorithm',
      icon: 'pi pi-bolt',
      color: 'text-secondary'
    },
    {
      number: 100,
      label: '🧡 Shelters Across Australia',
      description: 'Built for shelters across Australia and beyond',
      icon: 'pi pi-map-marker',
      color: 'text-orange-500'
    }
  ];

  testimonials = [
    {
      quote: "PawfectMatch has revolutionized how we connect pets with families. The matching algorithm is incredibly accurate and saves us so much time.",
      name: "Sarah M.",
      role: "Shelter Manager",
      avatar: "https://images.unsplash.com/photo-1494790108755-2616b612b886?w=150&h=150&fit=crop&crop=face"
    },
    {
      quote: "We found our perfect companion through PawfectMatch. The process was smooth, and the match was spot-on. Highly recommended!",
      name: "Mike D.",
      role: "Happy Adopter",
      avatar: "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face"
    },
    {
      quote: "As a volunteer, I love how PawfectMatch makes it easier to help pets find loving homes. The interface is intuitive and powerful.",
      name: "Emma L.",
      role: "Shelter Volunteer",
      avatar: "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150&h=150&fit=crop&crop=face"
    }
  ];

  currentTestimonialIndex = 0;
  private testimonialInterval: NodeJS.Timeout | null = null;

  ngOnInit() {
    // Check if user is already authenticated and redirect if needed
    this.authService.authStatus$.subscribe(status => {
      if (status.isAuthenticated) {
        this.router.navigate(['/dashboard']);
      }
    });
  }

  ngAfterViewInit() {
    if (isPlatformBrowser(this.platformId)) {
      // Small delay to ensure DOM is ready
      setTimeout(() => {
        this.initializeAnimations();
        this.animateCounters();
        this.setupButtonAnimations();
        this.startTestimonialCarousel();
      }, 100);
    }
  }

  private initializeAnimations() {
    // Hero section animations
    const tl = gsap.timeline();

    tl.from('.hero-headline', {
      duration: 1,
      y: 50,
      opacity: 0,
      ease: 'power3.out'
    })
    .from('.hero-subheadline', {
      duration: 0.8,
      y: 30,
      opacity: 0,
      ease: 'power3.out'
    }, '-=0.5')
    .from('.hero-buttons', {
      duration: 0.8,
      y: 30,
      opacity: 0,
      ease: 'power3.out'
    }, '-=0.3')
    .from('.hero-image img', {
      duration: 1.5,
      scale: 0.8,
      opacity: 0,
      ease: 'power3.out'
    }, '-=0.6');

    // Benefits section scroll animations
    gsap.fromTo('.benefit-card',
      {
        y: 60,
        opacity: 0
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
          toggleActions: 'play none none reverse'
        }
      }
    );

    // Stats section animations
    gsap.fromTo('.stat-card',
      {
        scale: 0.8,
        opacity: 0
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
          toggleActions: 'play none none reverse'
        }
      }
    );

    // Testimonials section animations
    gsap.fromTo('.testimonial-card',
      {
        y: 50,
        opacity: 0
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
          toggleActions: 'play none none reverse'
        }
      }
    );

    // Final CTA section with parallax effect
    gsap.fromTo('.final-cta-bg',
      {
        backgroundPosition: 'center 20%'
      },
      {
        backgroundPosition: 'center 80%',
        duration: 2,
        ease: 'none',
        scrollTrigger: {
          trigger: '.final-cta',
          start: 'top bottom',
          end: 'bottom top',
          scrub: 1
        }
      }
    );

    // Floating CTA button scroll behavior
    ScrollTrigger.create({
      trigger: '.primary-cta',
      start: 'top center',
      end: 'bottom bottom',
      onEnter: () => {
        gsap.to('.floating-cta', {
          opacity: 1,
          y: 0,
          duration: 0.5,
          ease: 'power3.out'
        });
      },
      onLeave: () => {
        gsap.to('.floating-cta', {
          opacity: 0,
          y: 100,
          duration: 0.5,
          ease: 'power3.in'
        });
      },
      onEnterBack: () => {
        gsap.to('.floating-cta', {
          opacity: 1,
          y: 0,
          duration: 0.5,
          ease: 'power3.out'
        });
      },
      onLeaveBack: () => {
        gsap.to('.floating-cta', {
          opacity: 0,
          y: 100,
          duration: 0.5,
          ease: 'power3.in'
        });
      }
    });
  }

  private animateCounters() {
    this.stats.forEach((stat, index) => {
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
                  element.innerHTML = currentValue.toString();
                }
              },
              onComplete: () => {
                // Add a little bounce effect when counter finishes
                gsap.to(element, {
                  scale: 1.1,
                  duration: 0.3,
                  yoyo: true,
                  repeat: 1,
                  ease: "power2.inOut"
                });
              }
            });
          }
        });
      }
    });
  }

  private setupButtonAnimations() {
    // CTA button hover animations
    const buttons = document.querySelectorAll('.animated-button');

    buttons.forEach(button => {
      button.addEventListener('mouseenter', () => {
        gsap.to(button, {
          scale: 1.05,
          duration: 0.3,
          ease: 'power2.out'
        });
      });

      button.addEventListener('mouseleave', () => {
        gsap.to(button, {
          scale: 1,
          duration: 0.3,
          ease: 'power2.out'
        });
      });
    });

    // Pulse animation for primary CTA
    gsap.to('.cta-pulse', {
      scale: 1.05,
      duration: 1.5,
      repeat: -1,
      yoyo: true,
      ease: 'power2.inOut'
    });
  }

  private startTestimonialCarousel() {
    this.testimonialInterval = setInterval(() => {
      this.nextTestimonial();
    }, 5000);
  }

  nextTestimonial() {
    if (this.testimonials.length > 1) {
      this.currentTestimonialIndex = (this.currentTestimonialIndex + 1) % this.testimonials.length;

      // Animate testimonial change
      gsap.fromTo('.active-testimonial',
        { opacity: 0, x: 50 },
        { opacity: 1, x: 0, duration: 0.5, ease: 'power2.out' }
      );
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
    img.src = 'https://via.placeholder.com/600x400/f3f4f6/6b7280?text=Happy+Pet+Family';
  }

  onImageLoad(event: Event) {
    const img = event.target as HTMLImageElement;
    gsap.from(img, {
      opacity: 0,
      scale: 0.95,
      duration: 0.8,
      ease: 'power2.out'
    });
  }

  ngOnDestroy() {
    if (this.testimonialInterval) {
      clearInterval(this.testimonialInterval);
    }
  }
}
