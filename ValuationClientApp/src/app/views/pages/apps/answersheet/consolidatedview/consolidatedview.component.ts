import {
  AfterViewInit,
  OnInit,
  Component,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import {
  NgbDropdownModule,
  NgbNavModule,
  NgbTooltip,
  NgbModal,
  NgbModalRef,
} from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormArray,
  Validators,
  FormsModule,
  ReactiveFormsModule,
  FormControl,
} from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { AnswersheetService } from '../../../services/answersheet.service';
import { InstituteService } from '../../../services/institute.service';
import { TemplateManagementService } from '../../../services/template-management.service';
import { ThirdPartyDraggable } from '@fullcalendar/interaction/index.js';
import { SpinnerService } from '../../../services/spinner.service';

@Component({
  selector: 'app-consolidatedview',
  imports: [
    NgbNavModule,
    NgbDropdownModule,
    NgScrollbarModule,
    NgbTooltip,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatCheckboxModule,
  ],
  templateUrl: './consolidatedview.component.html',
  styleUrl: './consolidatedview.component.scss',
})
export class ConsolidatedviewComponent {
  allocateAnswersheetForm!: FormGroup;
  allocateTransactionInprogress: boolean = false;

  dataSourceExamYears: any[] = [];
  dataSourceExamMonths: any[] = [];
  dataSourceExamTypes: any[] = [];

  selectedExamYear: string = '';
  selectedExamMonth: string = '';
  selectedExamType: string = '';

  ddlType = new FormControl('');

  modalRef: NgbModalRef;
  gridDataItems: any[] = [];
  dataSource = new MatTableDataSource<any>([]);
  institutes: any[] = [];
  selectedElement: any;
  users: any[] = [];
  enterAllocation: any;
  displayedColumns: string[] = [
    'courseCode',
    'courseName',
    'studentTotalCount',
    'answerSheetTotalCount',
    'answerSheetAllocatedCount',
    'answerSheetNotAllocatedCount',
    'actions',
  ];

  @ViewChild('allocateModal') allocateModal: any;
  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  constructor(
    private fb: FormBuilder,
    private modalService: NgbModal,
    private answersheetService: AnswersheetService,
    private templateService: TemplateManagementService,
    private spinnerService: SpinnerService
  ) {
    this.allocateAnswersheetForm = this.fb.group({
      userId: ['0', Validators.required],
      noOfAnswersheetsAllocated: ['0', Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadAllExamYears();
    this.loadAllExamMonths();
    this.loadAllExamTypes();
    this.loadExperts();
    this.gridDataItems = [];
  }

  loadAllExamYears(): void {
    this.answersheetService.getExamYears().subscribe({
      next: (data) => {
        this.dataSourceExamYears = data;
        console.log('ExamYears loaded:', this.dataSourceExamYears);
      },
      error: (err) => {
        console.error('Error loading ExamYears:', err);
      },
    });
  }

  loadAllExamMonths(): void {
    this.answersheetService.getExamMonths().subscribe({
      next: (data: any) => {
        this.dataSourceExamMonths = data;
        console.log('ExamMonths loaded:', this.dataSourceExamMonths);
      },
      error: (err: any) => {
        console.error('Error loading ExamMonths:', err);
      },
    });
  }

  loadAllExamTypes(): void {
    this.answersheetService.getExamTypes().subscribe({
      next: (data: any) => {
        this.dataSourceExamTypes = data;
        console.log('ExamTypes loaded:', this.dataSourceExamMonths);
      },
      error: (err: any) => {
        console.error('Error loading ExamTypes:', err);
      },
    });
    this.ddlType.setValue('0');
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  loadGridData(): void {
    this.answersheetService
      .getConsolidatedExamAnswersheets(this.selectedExamYear, this.selectedExamMonth, this.selectedExamType, 0)
      .subscribe({
        next: (data: any[]) => {
          this.gridDataItems = data;
          this.dataSource.data = this.gridDataItems;
          this.dataSource.paginator = this.paginator;
          this.dataSource.sort = this.sort;
          // console.log('history data:', this.answersheets);
        },
        error: (errRes: any) => {
          console.error('Error loading history:', errRes);
        },
      });
  }

  onExamYearChange(event: Event): void {
    this.selectedExamYear = (event.target as HTMLSelectElement).value;
    this.ddlType.setValue('0');
  }

  onExamMonthChange(event: Event): void {
    this.selectedExamMonth = (event.target as HTMLSelectElement).value;
    this.ddlType.setValue('0');
  }

  onExamTypeChange(event: Event): void {
    this.selectedExamType = (event.target as HTMLSelectElement).value;
    this.loadGridData();
  }

  loadExperts(): void {
    this.templateService.getExpertsForQPAssignment().subscribe({
      next: (data) => {
        this.users = data.filter((user: any) => user.userId != 1);
        console.log('Users loaded:', this.users);
      },
      error: (error) => {
        console.error('Error loading users:', error);
      },
    });
  }

  disableIfAdmin(userId: number) {
    if (userId == 1) {
      return true;
    } else {
      return false;
    }
  }

  closeAllocationPopup() {
    this.modalRef.dismiss();
    this.allocateTransactionInprogress = false;
  }

  IsValidNoofScripts() {
    if (
      parseInt(this.allocateAnswersheetForm.value.noOfAnswersheetsAllocated) >
      this.selectedElement.answerSheetNotAllocatedCount
    ) {
      return false;
    }
    return true;
  }

  saveAllocation() {
    if (
      parseInt(this.allocateAnswersheetForm.value.noOfAnswersheetsAllocated) <=
      this.selectedElement.answerSheetNotAllocatedCount
    ) {
      this.spinnerService.toggleSpinnerState(true);
      this.allocateTransactionInprogress = true;
      this.answersheetService
        .AllocateAnswerSheetsToUser(
          this.selectedElement.examinationId,
          parseInt(this.allocateAnswersheetForm.value.userId),
          parseInt(this.allocateAnswersheetForm.value.noOfAnswersheetsAllocated)
        )
        .subscribe(
          (data: any) => {
            this.spinnerService.toggleSpinnerState(false);
            this.loadGridData();
            this.closeAllocationPopup();
          },
          (error) => {
            this.spinnerService.toggleSpinnerState(false);
            this.closeAllocationPopup();
          }
        );
    }
  }

  updateAllocation(user: any) {
    console.log(user.target?.value);
    if (user.target?.value) {
      this.enterAllocation = true;
    } else {
      this.enterAllocation = false;
    }
  }

  allocationDialog(entity: any) {
    this.selectedElement = entity;
    this.modalRef = this.modalService.open(this.allocateModal, {
      size: 'md',
      backdrop: 'static',
    });
    this.allocateTransactionInprogress = false;
    this.allocateAnswersheetForm.reset();
  }
}
