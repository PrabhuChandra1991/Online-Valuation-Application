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
  NgbModalRef
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
  styleUrl: './consolidatedview.component.scss'
})
export class ConsolidatedviewComponent {

  allocateAnswersheetForm!: FormGroup;
  allocateTransactionInprogress: boolean = false;

  modalRef: NgbModalRef;
  gridDataItems: any[] = [];
  dataSource = new MatTableDataSource<any>([]);
  institutes: any[] = [];
  selectedInstituteId: string = '';
  selectedElement: any;
  users: any[] = [];
  enterAllocation: any;
  displayedColumns: string[] = [
    'institutionCode',
    'regulationYear',
    'batchYear',
    'degreeType',
    'examType',
    'semester',
    'courseCode',
    'examMonth',
    'examYear',
    'studentTotalCount',
    'answerSheetTotalCount',
    'answerSheetAllocatedCount',
    'answerSheetNotAllocatedCount',
    'actions'
  ];

  @ViewChild('allocateModal') allocateModal: any;
  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;

  constructor(
    private fb: FormBuilder,
    private modalService: NgbModal,
    private answersheetService: AnswersheetService,
    private instituteService: InstituteService,
    private templateService: TemplateManagementService
  ) {

    this.allocateAnswersheetForm = this.fb.group({
      userId: ['0', Validators.required],
      noOfAnswersheetsAllocated: ['0', Validators.required]
    })

  }

  ngOnInit(): void {
    this.loadAllInstitues();
    this.loadExperts();
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
      .getConsolidatedExamAnswersheets(parseInt(this.selectedInstituteId))
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

  onInstituteChange(event: Event): void {
    this.selectedInstituteId = (event.target as HTMLSelectElement).value;
    this.loadGridData();
  }

  loadAllInstitues(): void {
    this.instituteService.getInstitutions().subscribe({
      next: (data) => {
        this.institutes = data;
        console.log('Institutes loaded:', this.institutes);
      },
      error: (error) => {
        console.error('Error loading Institutes:', error);
      },
    });
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
    }
    else {
      return false;
    }
  }


  closeAllocationPopup() {
    this.modalRef.dismiss();
    this.allocateTransactionInprogress = false;
  }

  saveAllocation() {
    this.allocateTransactionInprogress = true;
    this.answersheetService.AllocateAnswerSheetsToUser(
      this.selectedElement.examinationId,
      parseInt(this.allocateAnswersheetForm.value.userId),
      parseInt(this.allocateAnswersheetForm.value.noOfAnswersheetsAllocated)).subscribe(
        (data: any) => {
          console.log("save data: ", data)
          if (data.message.toLowerCase() == 'success') {
            // this.toastr.success("Mark auto saved.");
          }
          this.closeAllocationPopup();
        },
        (error) => {
          console.error('Error saving mark:', error);
          // this.toastr.error('Failed to save mark.');
          this.closeAllocationPopup();
        }
      )
  }

  updateAllocation(user: any) {
    console.log(user.target?.value)
    if (user.target?.value) {
      this.enterAllocation = true;
    }
    else {
      this.enterAllocation = false;
    }
  }

  allocationDialog(entity: any) {
    this.selectedElement = entity;
    this.modalRef = this.modalService.open(this.allocateModal, { size: 'md', backdrop: 'static' });
    this.allocateTransactionInprogress = false;
  }

}
