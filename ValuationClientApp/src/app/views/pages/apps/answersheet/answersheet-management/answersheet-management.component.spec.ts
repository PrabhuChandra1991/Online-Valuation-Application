import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AnswersheetManagementComponent } from './answersheet-management.component';

describe('AnswersheetManagementComponent', () => {
  let component: AnswersheetManagementComponent;
  let fixture: ComponentFixture<AnswersheetManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AnswersheetManagementComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AnswersheetManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
