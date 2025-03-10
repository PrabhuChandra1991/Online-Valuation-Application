import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { NgbCalendar, NgbDatepickerModule, NgbDateStruct, NgbDropdownModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { FormBuilder, FormGroup, FormArray, Validators, FormsModule, ReactiveFormsModule, FormControl } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { FeatherIconDirective } from '../../../core/feather-icon/feather-icon.directive';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { UserService } from '../services/user.service';
import { MatIcon } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import {MatButtonModule} from '@angular/material/button';

@Component({
  selector: 'app-dashboard',
  imports: [
    FormsModule,
    ReactiveFormsModule,  // Ensure this is present
    NgbDropdownModule,
    NgbDatepickerModule,
    NgApexchartsModule,
    FeatherIconDirective,
    CommonModule,
    MatIcon,
    MatCheckboxModule,
    MatButtonModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {

  userForm!: FormGroup;
  designationForm!: FormGroup;
  specializationForm!: FormGroup;
  selectedDesignationIndex: number | null = null;
  selectedSpecializationIndex: number | null = null;


  @ViewChild('editSpecializationModal') editSpecializationModal!: TemplateRef<any>;
  @ViewChild('editDesignationModal') editDesignationModal!: TemplateRef<any>;

  departments = [{ id: 1, name: "Computer Science" }, { id: 2, name: "Mechanical" }, { id: 3, name: "Electrical" }, { id: 4, name: "Civil" }, { id: 5, name: "Electronics" }];
  designations = [{ id: 1, name: "Professor" }, { id: 2, name: "Associate Professor" }, { id: 3, name: "Assistant Professor" }];

  // Local storage of Specialization & Qualification
  specializations: any[] = [];
  designation: any[] = [];

  constructor(
    private fb: FormBuilder,
    private modalService: NgbModal,
    private calendar: NgbCalendar,
    private userService: UserService,
    private toastr: ToastrService
  ) {
    // Initialize the form with default values
    this.specializationForm = new FormGroup({
      course: new FormControl(''),
      // experience: new FormControl(''),
      // handledLastTwoSemesters: new FormControl(false),
    }),

    this.designationForm = new FormGroup({
      expDesignation: new FormControl('', Validators.required),
      experience: new FormControl('', Validators.required),
      isCurrent: new FormControl(false),
          
    });
  }

  ngOnInit(): void {
    this.userForm = this.fb.group({
      name: [{ value: '', disabled: true }, Validators.required],
      email: [{ value: '', disabled: true }, [Validators.required, Validators.email]],
      mobileNumber: [{ value: '', disabled: true }, [Validators.required, Validators.pattern('^[0-9]{10}$')]],
      departmentId: ['', Validators.required],
      designationId: ['', Validators.required],
      collegeName: ['', Validators.required],
      bankAccountName: [''],
      bankAccountNumber: ['', Validators.pattern('^[0-9]+$')],
      bankBranchName: [''],
      bankIfscCode: [''],
      specializations: this.fb.array([]),
      designation: this.fb.array([]),
      ug: ['', [Validators.required]],
      pg: ['' , [Validators.required]],
      hasPhd: [false]  // Checkbox for PhD completion
    });

    // Example specializations to show in the table initially
    this.specializations = [
      {
        userAreaOfSpecializationId: 1,
        userId: 4,
        course: "Computer Science",
        experience: 5,
        handledLastTwoSemesters: true
      },
      {
        userAreaOfSpecializationId: 2,
        userId: 4,
        course: "Mathematics",
        experience: 3,
        handledLastTwoSemesters: true
      },
      {
        userAreaOfSpecializationId: 3,
        userId: 4,
        course: "Physics",
        experience: 1,
        handledLastTwoSemesters: true
      }
    ];

    // this.designation = [
    //   {
    //     education: 'B.Sc',
    //     course: 'Computer Science',
    //     specialization: 'Artificial Intelligence'
    //   },
    //   {
    //     education: 'M.Sc',
    //     course: 'Software Engineering',
    //     specialization: 'Machine Learning'
    //   },
    //   {
    //     education: 'MS',
    //     course: 'Data Science',
    //     specialization: 'Deep Learning'
    //   }
    // ];
    

    this.loadUserData();
  }

  // Load User Data from Local Storage
  loadUserData() {
    const loggedData = localStorage.getItem('userData');
    if (loggedData) {
      const userData = JSON.parse(loggedData);
      this.userForm.patchValue({
        name: userData.name,
        email: userData.email,
        mobileNumber: userData.mobileNumber,
        collegeName: userData.collegeName,
        bankAccountName: userData.bankAccountName,
        bankAccountNumber: userData.bankAccountNumber,
        bankBranchName: userData.bankBranchName
      });
    }
  }
/****************************Specilization *************************************************************************/
  // Open Specialization Modal
  openSpecializationModal() {
    const modalRef = this.modalService.open(this.editSpecializationModal);
    modalRef.result.then((result) => {
      if (result) {
        this.specializations.push(result); // Add the new specialization
      }
    }).catch(() => { });
  }

  saveSpecialization() {
    if (this.selectedSpecializationIndex !== null) {
      // Update the existing specialization
      this.specializations[this.selectedSpecializationIndex] = this.specializationForm.value;
    } else {
      // If no specialization is selected, add a new one
      this.specializations.push(this.specializationForm.value);
    }
    
    this.modalService.dismissAll(); // Close the modal
    this.specializationForm.reset(); // Reset the form
    this.selectedSpecializationIndex = null; // Clear the selection
  }

  // Open Specialization Modal for editing
  editSpecialization(index: number) {
    this.selectedSpecializationIndex = index;  // Store the index of the specialization being edited
    const specialization = this.specializations[index];

    // Set the form values to the clicked specialization data
    this.specializationForm.patchValue({
      course: specialization.course
      // experience: specialization.experience,
      // handledLastTwoSemesters: specialization.handledLastTwoSemesters
    });

    // Open the modal
    const modalRef = this.modalService.open(this.editSpecializationModal);
    modalRef.result.then((result) => {
      if (result) {
        if (this.selectedSpecializationIndex !== null) {
          // Update the specialization with the edited values
          this.specializations[this.selectedSpecializationIndex] = this.specializationForm.value;
        }
      }
    }).catch(() => {});
  }

  // Delete Specialization Entry
  deleteSpecialization(index: number) {
    this.specializations.splice(index, 1);
  }

 /****************************Specilization section end**************************************************************/


/****************************qualification *************************************************************************/
openDesignationModal() {
  const modalRef = this.modalService.open(this.editDesignationModal);
  modalRef.result.then((result) => {
    if (result) {
      this.designation.push(result); // Add the new Qualification
    }
  }).catch(() => { });
}

saveExperience() {
  if (this.selectedDesignationIndex !== null) {
    // Update the existing Qualification
    this.designation[this.selectedDesignationIndex] = this.designationForm.value;
  } else {
    // If no Qualification is selected, add a new one
    this.designation.push(this.designationForm.value);
  }
  
  this.modalService.dismissAll(); // Close the modal
  this.designationForm.reset(); // Reset the form
  this.selectedDesignationIndex = null; // Clear the selection
}

// Open Qualification Modal for editing
editQualification(index: number) {
  this.selectedDesignationIndex = index;  // Store the index of the Qualification being edited
  const Designation = this.designation[index];

  // Set the form values to the clicked Qualification data
  this.designationForm.patchValue({
    expDesignationId: Designation.expDesignationId,
    experience: Designation.experience,
    isCurrent: Designation.isCurrent
  });

  // Open the modal
  const modalRef = this.modalService.open(this.editDesignationModal);
  modalRef.result.then((result) => {
    if (result) {
      if (this.selectedDesignationIndex !== null) {
        // Update the Qualification with the edited values
        this.designation[this.selectedDesignationIndex] = this.designationForm.value;
      }
    }
  }).catch(() => {});
}

// Delete Qualification Entry
deleteQualification(index: number) {
  this.designation.splice(index, 1);
}
 /****************************qualification section end*************************************************************/

  // Submit Form including Specialization & Qualification
  onSubmit() {
    if (this.userForm.valid) {
      const formData = {
        ...this.userForm.value,
        specializations: this.specializations,
        designation: this.designation
      };

      this.updateUser(formData);
      console.log('Submitted Data:', formData);
    } else {
      console.log('Form is invalid');
    }
  }

  updateUser(userData: any) {
    this.userService.updateUser("3", userData).subscribe({
      next: () => {
        this.toastr.success('User updated successfully!');
      },
      error: () => {
        this.toastr.error('Failed to update user. Please try again.');
      },
      complete: () => { }
    });
  }
}

