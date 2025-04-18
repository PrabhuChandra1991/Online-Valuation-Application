import { JsonPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgbCalendar, NgbDate, NgbDateParserFormatter, NgbDatepickerModule, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { CodePreviewComponent } from '../../../partials/code-preview/code-preview.component';

const defaultDatepicker = {
  htmlCode: 
`<ngb-datepicker #dp [(ngModel)]="selectedDate" (navigate)="date = $event.next"></ngb-datepicker>
<hr>
<button class="btn btn-sm btn-primary me-1" (click)="selectToday()">Select Today</button>
<button class="btn btn-sm btn-primary me-1" (click)="dp.navigateTo()">To current month</button>
<button class="btn btn-sm btn-primary me-1" (click)="dp.navigateTo({year: 2013, month: 2})">To Feb 2013</button>
<hr>
<p>Month: {{ date.month }}.{{ date.year }}</p>
<p>Selected: {{ selectedDate | json }}</p>`,
  tsCode: 
`import { Component, inject } from '@angular/core';
import { NgbDateStruct, NgbCalendar } from '@ng-bootstrap/ng-bootstrap';
import { NgbCalendar, NgbDatepickerModule, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { FormsModule } from '@angular/forms';
import { JsonPipe } from '@angular/common';

@Component({
  selector: 'app-datepicker',
  standalone: true,
  imports: [NgbDatepickerModule, FormsModule, JsonPipe],
  templateUrl: './datepicker.component.html'
})
export class DatepickerComponent {
  calendar = inject(NgbCalendar);

  selectedDate: NgbDateStruct;
  date: {year: number, month: number};

  constructor() {}

  selectToday() {
    this.selectedDate = this.calendar.getToday();
  }
}`
}

const inPopup = {
  htmlCode: 
`<form class="d-flex">
  <div class="input-group">
    <input class="form-control" placeholder="yyyy-mm-dd"
        name="dp" [(ngModel)]="selectedDate" ngbDatepicker #d="ngbDatepicker">
    <button class="input-group-text" type="button" (click)="d.toggle()">
      <i class="feather icon-calendar icon-md text-secondary"></i>
    </button>
  </div>
</form>
<hr>
<p class="mt-3 text-secondary">Selected: {{ selectedDate | json }}</p>`,
  tsCode: 
`import { Component } from '@angular/core';
import { NgbAlertModule, NgbDatepickerModule, NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { FormsModule } from '@angular/forms';
import { JsonPipe } from '@angular/common';

@Component({
  selector: 'app-datepicker',
  standalone: true,
  imports: [NgbDatepickerModule, FormsModule, JsonPipe],
  templateUrl: './datepicker.component.html'
})
export class DatepickerComponent {

  selectedDate: NgbDateStruct;

}`
}

const rangeSelection = {
  htmlCode: 
`<ngb-datepicker class="range-selection" #dp (dateSelect)="onDateSelection($event)" [displayMonths]="2" [dayTemplate]="t" outsideDays="hidden">
</ngb-datepicker>

<ng-template #t let-date let-focused="focused">
  <span class="custom-day"
        [class.focused]="focused"
        [class.range]="isRange(date)"
        [class.faded]="isHovered(date) || isInside(date)"
        (mouseenter)="hoveredDate = date"
        (mouseleave)="hoveredDate = null">
    {{ date.day }}
  </span>
</ng-template>
<p class="mt-3 text-secondary">From: {{ fromDate | json }}</p>
<p class="text-secondary">To: {{ toDate | json }}</p>`,
  tsCode: 
`import { Component, inject } from '@angular/core';
import { NgbCalendar, NgbDate, NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';
import { FormsModule } from '@angular/forms';
import { JsonPipe } from '@angular/common';

@Component({
  selector: 'app-datepicker',
  standalone: true,
  imports: [NgbDatepickerModule, FormsModule, JsonPipe],
  templateUrl: './datepicker.component.html'
})
export class DatepickerComponent {
  calendar = inject(NgbCalendar);

  hoveredDate: NgbDate | null = null;
  fromDate: NgbDate | null = this.calendar.getToday();
  toDate: NgbDate | null = this.calendar.getNext(this.calendar.getToday(), 'd', 10);

  constructor() {}

  onDateSelection(date: NgbDate) {
    if (!this.fromDate && !this.toDate) {
      this.fromDate = date;
    } else if (this.fromDate && !this.toDate && date.after(this.fromDate)) {
      this.toDate = date;
    } else {
      this.toDate = null;
      this.fromDate = date;
    }
  }

  isHovered(date: NgbDate) {
    return this.fromDate && !this.toDate && this.hoveredDate && date.after(this.fromDate) && date.before(this.hoveredDate);
  }

  isInside(date: NgbDate) {
    return this.toDate && date.after(this.fromDate) && date.before(this.toDate);
  }

  isRange(date: NgbDate) {
    return date.equals(this.fromDate) || (this.toDate && date.equals(this.toDate)) || this.isInside(date) || this.isHovered(date);
  }
}`
}

const rangeSelectionPopup = {
  htmlCode: 
`<form class="d-flex range-selection">
  <div class="form-group hidden">
    <div class="input-group">
      <input name="datepicker"
            class="form-control"
            ngbDatepicker
            #datepicker="ngbDatepicker"
            [autoClose]="'outside'"
            (dateSelect)="onDateSelection($event)"
            [displayMonths]="2"
            [dayTemplate]="t"
            outsideDays="hidden"
            [startDate]="fromDate!">
      <ng-template #t let-date let-focused="focused">
        <span class="custom-day"
              [class.focused]="focused"
              [class.range]="isRange(date)"
              [class.faded]="isHovered(date) || isInside(date)"
              (mouseenter)="hoveredDate = date"
              (mouseleave)="hoveredDate = null">
          {{ date.day }}
        </span>
      </ng-template>
    </div>
  </div>
  <div class="mb-3">
    <div class="input-group" (click)="datepicker.toggle()">
      <input #dpFromDate
            class="form-control" placeholder="yyyy-mm-dd"
            name="dpFromDate"
            [value]="formatter.format(fromDate)"
            (input)="fromDate = validateInput(fromDate, dpFromDate.value)">
      <button class="input-group-text" type="button">
        <i class="feather icon-calendar icon-md text-secondary"></i>
      </button>
    </div>
  </div>
  <div class="ms-2">
    <div class="input-group" (click)="datepicker.toggle()">
      <input #dpToDate
            class="form-control" placeholder="yyyy-mm-dd"
            name="dpToDate"
            [value]="formatter.format(toDate)"
            (input)="toDate = validateInput(toDate, dpToDate.value)">
      <button class="input-group-text" type="button">
        <i class="feather icon-calendar icon-md text-secondary"></i>
      </button>
    </div>
  </div>
</form>
<p class="mt-3 text-secondary">From: {{ fromDate | json }}</p>
<p class="text-secondary">To: {{ toDate | json }}</p>`,
  tsCode: 
`import { Component, inject } from '@angular/core';
import { NgbCalendar, NgbDate, NgbDateParserFormatter, NgbDatepickerModule } from '@ng-bootstrap/ng-bootstrap';
import { FormsModule } from '@angular/forms';
import { JsonPipe } from '@angular/common';

@Component({
  selector: 'app-datepicker',
  standalone: true,
  imports: [NgbDatepickerModule, FormsModule, JsonPipe],
  templateUrl: './datepicker.component.html'
})
export class DatepickerComponent {
  calendar = inject(NgbCalendar);
  formatter = inject(NgbDateParserFormatter);

  hoveredDate: NgbDate | null = null;
  fromDate: NgbDate | null = this.calendar.getToday();
  toDate: NgbDate | null = this.calendar.getNext(this.calendar.getToday(), 'd', 10);

  constructor() {}

  onDateSelection(date: NgbDate) {
    if (!this.fromDate && !this.toDate) {
      this.fromDate = date;
    } else if (this.fromDate && !this.toDate && date && date.after(this.fromDate)) {
      this.toDate = date;
    } else {
      this.toDate = null;
      this.fromDate = date;
    }
  }

  isHovered(date: NgbDate) {
    return this.fromDate && !this.toDate && this.hoveredDate && date.after(this.fromDate) && date.before(this.hoveredDate);
  }

  isInside(date: NgbDate) {
    return this.toDate && date.after(this.fromDate) && date.before(this.toDate);
  }

  isRange(date: NgbDate) {
    return date.equals(this.fromDate) || (this.toDate && date.equals(this.toDate)) || this.isInside(date) || this.isHovered(date);
  }

  validateInput(currentValue: NgbDate | null, input: string): NgbDate | null {
    const parsed = this.formatter.parse(input);
    return parsed && this.calendar.isValid(NgbDate.from(parsed)) ? NgbDate.from(parsed) : currentValue;
  }

}`
}

@Component({
    selector: 'app-datepicker',
    imports: [
        CodePreviewComponent,
        NgbDatepickerModule,
        FormsModule,
        JsonPipe
    ],
    templateUrl: './datepicker.component.html'
})
export class DatepickerComponent implements OnInit {

  selectedDate: NgbDateStruct;
  date: {year: number, month: number};

  calendar = inject(NgbCalendar);
  formatter = inject(NgbDateParserFormatter);

  hoveredDate: NgbDate | null = null;
  fromDate: NgbDate | null = this.calendar.getToday();
  toDate: NgbDate | null = this.calendar.getNext(this.calendar.getToday(), 'd', 10);

  defaultDatepickerCode: any;
  inPopupCode: any;
  rangeSelectionCode: any;
  rangeSelectionPopupCode: any;

  constructor() {}

  ngOnInit(): void {
    this.defaultDatepickerCode = defaultDatepicker;
    this.inPopupCode = inPopup;
    this.rangeSelectionCode = rangeSelection;
    this.rangeSelectionPopupCode = rangeSelectionPopup;
  }

  selectToday(): void {
    this.selectedDate = this.calendar.getToday()
  }

  scrollTo(element: any) {
    element.scrollIntoView({behavior: 'smooth'});
  }


  onDateSelection(date: NgbDate) {
    if (!this.fromDate && !this.toDate) {
      this.fromDate = date;
    } else if (this.fromDate && !this.toDate && date && date.after(this.fromDate)) {
      this.toDate = date;
    } else {
      this.toDate = null;
      this.fromDate = date;
    }
  }

  isHovered(date: NgbDate) {
    return this.fromDate && !this.toDate && this.hoveredDate && date.after(this.fromDate) && date.before(this.hoveredDate);
  }

  isInside(date: NgbDate) {
    return this.toDate && date.after(this.fromDate) && date.before(this.toDate);
  }

  isRange(date: NgbDate) {
    return date.equals(this.fromDate) || (this.toDate && date.equals(this.toDate)) || this.isInside(date) || this.isHovered(date);
  }

  validateInput(currentValue: NgbDate | null, input: string): NgbDate | null {
    const parsed = this.formatter.parse(input);
    return parsed && this.calendar.isValid(NgbDate.from(parsed)) ? NgbDate.from(parsed) : currentValue;
  }

}
