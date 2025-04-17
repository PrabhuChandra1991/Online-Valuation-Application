import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConsolidatedviewComponent } from './consolidatedview.component';

describe('ConsolidatedviewComponent', () => {
  let component: ConsolidatedviewComponent;
  let fixture: ComponentFixture<ConsolidatedviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConsolidatedviewComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ConsolidatedviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
