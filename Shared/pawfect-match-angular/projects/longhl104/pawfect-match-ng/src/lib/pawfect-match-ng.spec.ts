import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PawfectMatchNg } from './pawfect-match-ng';

describe('PawfectMatchNg', () => {
  let component: PawfectMatchNg;
  let fixture: ComponentFixture<PawfectMatchNg>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PawfectMatchNg]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PawfectMatchNg);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
