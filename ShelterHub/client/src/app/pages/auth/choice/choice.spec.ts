import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Choice } from './choice';

describe('Choice', () => {
  let component: Choice;
  let fixture: ComponentFixture<Choice>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Choice]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Choice);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
