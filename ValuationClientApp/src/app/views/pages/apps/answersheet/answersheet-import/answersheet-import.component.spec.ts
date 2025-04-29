import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AnswersheetImportComponent } from './answersheet-import.component';

describe('AnswersheetImportComponent', () => {
  let component: AnswersheetImportComponent;
  let fixture: ComponentFixture<AnswersheetImportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AnswersheetImportComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AnswersheetImportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
