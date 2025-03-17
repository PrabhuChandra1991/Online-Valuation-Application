import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TemplateManagemenComponent } from './template-management.component';

describe('TemplateListComponent', () => {
  let component: TemplateManagemenComponent;
  let fixture: ComponentFixture<TemplateManagemenComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TemplateManagemenComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TemplateManagemenComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
