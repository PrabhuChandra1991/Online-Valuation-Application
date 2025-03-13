import { Component, OnInit, TemplateRef, viewChild, ViewChild } from '@angular/core';
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
import { ActivatedRoute } from '@angular/router';
import { DashboardService } from '../services/dashboard.service';
import { UserAreaOfSpecialization  } from '../models/userAreaOfSpecialization.model';
import { UserDesignation } from '../models/userDesignation.model';

const currentDate = new Date().toISOString();

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
  userCourseForm!: FormGroup;
  selectedDesignationIndex: number | null = null;
  selectedSpecializationIndex: number | null = null;
  selectedUserCourseIndex: number | null = null;
  selectedUserId:any;
  userQualifications: any[] = [];
  userCourses :any [] = [];
  Salutation:any;

  @ViewChild('editSpecializationModal') editSpecializationModal!: TemplateRef<any>;
  @ViewChild('editDesignationModal') editDesignationModal!: TemplateRef<any>;
  @ViewChild('editUserCourseModal') editUserCourseModal!: TemplateRef<any>;

  //this should be load from dashboard service 
  salutations = [
    { id: 'mr', name: 'Mr.' },
    { id: 'ms', name: 'Ms.' },
    { id: 'mrs', name: 'Mrs.' },
    { id: 'dr', name: 'Dr.' },
    { id: 'prof', name: 'Prof.' }
  ];

  genders = [
    { id: '1', name: 'Male' },
    { id: '2', name: 'Female' },
    { id: '3', name: 'Other' }
  ];

  degreeTypes = [
    {
      DegreeTypeId: 1,
      Name: 'UG',
      Code: 'UG',
      IsActive: 1,
      CreatedDate: '2025-03-11 18:08:23.823',
      CreatedById: 1,
      ModifiedDate: '2025-03-11 18:08:23.823',
      ModifiedById: 1
    },
    {
      DegreeTypeId: 2,
      Name: 'PG',
      Code: 'PG',
      IsActive: 1,
      CreatedDate: '2025-03-11 18:08:23.823',
      CreatedById: 1,
      ModifiedDate: '2025-03-11 18:08:23.823',
      ModifiedById: 1
    },
    {
      DegreeTypeId: 3,
      Name: 'Ph.D',
      Code: 'Ph.D',
      IsActive: 1,
      CreatedDate: '2025-03-11 18:08:23.823',
      CreatedById: 1,
      ModifiedDate: '2025-03-11 18:08:23.823',
      ModifiedById: 1
    }
  ];

  designationTypes = [{ DesignationId: 1, Name: "Professor" }, { DesignationId: 2, Name: "Associate Professor" }, { DesignationId: 3, Name: "Assistant Professor" }];

  designations = this.designationTypes.map(designation => ({
    isActive: true,
    createdById: 0,
    createdDate: currentDate,
    modifiedById: 0,
    modifiedDate: currentDate,
    userDesignationId: 0,
    designationId: designation.DesignationId,
    userId: 0,
    experience: 0,
    isCurrent: true,
    Name: designation.Name
  }));
  // Local storage of Specialization & Qualification
  specializations: any[] = [];

  constructor(
    private fb: FormBuilder,
    private modalService: NgbModal,
    private calendar: NgbCalendar,
    private userService: UserService,
    private toastr: ToastrService,
    private route: ActivatedRoute,
    private dashboardService: DashboardService
  ) {
    // Initialize the form with default values
    this.specializationForm = new FormGroup({
      areaOfSpecializationName: new FormControl(''),
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

    this.route.paramMap.subscribe(params => {
      const userId = params.get('id'); // Get user ID from route
      if (userId) {
        this.selectedUserId = userId;
        this.getUser(userId); // Load user data for editing
      }
    });


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
      designations: this.fb.array([]),
      ug: ['', [Validators.required]],
      pg: ['' , [Validators.required]],
      hasPhd: [false], // Checkbox for PhD completion,
      salutationId:['', Validators.required],
      genderId:['', Validators.required],
      department:['', Validators.required],
    });

    // Example specializations to show in the table initially
    this.specializations = [
      {
        isActive: true,
        createdById: 0,
        createdDate: new Date().toISOString(),
        modifiedById: 0,
        modifiedDate: new Date().toISOString(),
        userAreaOfSpecializationId: 1,
        userId: 4,
        areaOfSpecializationName: "Computer Science",
        experience: 5,
        handledLastTwoSemesters: true,
        user: ""
      },
      {
        isActive: true,
        createdById: 0,
        createdDate: new Date().toISOString(),
        modifiedById: 0,
        modifiedDate: new Date().toISOString(),
        userAreaOfSpecializationId: 2,
        userId: 4,
        areaOfSpecializationName: "Mathematics",
        experience: 3,
        handledLastTwoSemesters: true,
        user: ""
      },
      {
        isActive: true,
        createdById: 0,
        createdDate: new Date().toISOString(),
        modifiedById: 0,
        modifiedDate: new Date().toISOString(),
        userAreaOfSpecializationId: 3,
        userId: 4,
        areaOfSpecializationName: "Physics",
        experience: 1,
        handledLastTwoSemesters: true,
        user: ""
      }
    ];
  

    // Initialize the form with validation
    this.userCourseForm = this.fb.group({
      courseName: ['', Validators.required],
      degreeTypeId: ['', Validators.required],
      numberOfYearsHandled: ['', [Validators.required, Validators.min(1)]],
      isHandledInLast2Semester: [false]
    });

    //getting master data
    this.degreeTypes = this.getDegreeTypes();
    this.designations = this.getdesignations();
    this.loadUserData();
  }
  
  getDegreeTypes() {
    // Replace this with your API call
    // For now, returning the static data
    return this.degreeTypes;
  }

  getdesignations(){
    // Replace this with your API call
    // For now, returning the static data
    return this.designations;
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
        bankBranchName: userData.bankBranchName,
        salutationId:userData.salutationId,
        genderId:userData.gender,
        department:userData.departmentName
      });
    }
  }

  getUser(userId:any)
  {
    this.userService.getUser(userId).subscribe({
      next: (data) => {
       let selectedUser = data;
       if(selectedUser)
       {
        this.userForm.patchValue({
          name: selectedUser.name,
          email: selectedUser.email,
          mobileNumber: selectedUser.mobileNumber,
          collegeName: selectedUser.collegeName,
          bankAccountName: selectedUser.bankAccountName,
          bankAccountNumber: selectedUser.bankAccountNumber,
          bankBranchName: selectedUser.bankBranchName,
          salutationId:selectedUser.salutation,
          genderId:selectedUser.gender,
          department:selectedUser.departmentName

          
        });

        
        //assign child elements dynamically
        selectedUser.userDesignations = [];
        this.designations = selectedUser.userDesignations;
        this.specializations = selectedUser.userAreaOfSpecializations;
        this.userQualifications = selectedUser.userQualifications;
        this.userCourses = selectedUser.userCourses; 
        

       }
        console.log('User Details:', selectedUser);
      },
      error: (err) => {
        console.error('Error fetching user:', err);
      }
    });
  }

//#region Specilization grid Section
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
      this.specializations[this.selectedSpecializationIndex].areaOfSpecializationName = this.specializationForm.value.areaOfSpecializationName;      
    this.selectedSpecializationIndex = 0; // Clear the selection
    } else {
      // If no specialization is selected, add a new one
      let newSpecialization: UserAreaOfSpecialization = {
        userAreaOfSpecializationId: 0,
        userId: this.selectedUserId,
        areaOfSpecializationName: this.specializationForm.value.areaOfSpecializationName,
        isActive: false,
        createdDate: new Date().toISOString(),  // Current timestamp
        createdById: 0,
        modifiedDate: new Date().toISOString(),
        modifiedById: 0
      };
      
      this.specializations.push(newSpecialization);
    }
    
    this.modalService.dismissAll(); // Close the modal
    this.specializationForm.reset(); // Reset the form
  }

  // Open Specialization Modal for editing
  editSpecialization(index: number) {
    this.selectedSpecializationIndex = index;  // Store the index of the specialization being edited
    const specialization = this.specializations[index];

    // Set the form values to the clicked specialization data
    this.specializationForm.patchValue({
      areaOfSpecializationName: specialization.areaOfSpecializationName
      // experience: specialization.experience,
      // handledLastTwoSemesters: specialization.handledLastTwoSemesters
    });

    // Open the modal
    const modalRef = this.modalService.open(this.editSpecializationModal);
    modalRef.result.then((result) => {
      if (result) {
        if (this.selectedSpecializationIndex !== null) {
          // Update the specialization with the edited values
          this.specializations[this.selectedSpecializationIndex] = this.specializationForm.value.areaOfSpecializationName;
        }
      }
    }).catch(() => {});
  }

  // Delete Specialization Entry
  deleteSpecialization(index: number) {
    this.specializations.splice(index, 1);
  }

//#endregion

//#region Designation grid Section

openDesignationModal() {
  const modalRef = this.modalService.open(this.editDesignationModal);
  modalRef.result.then((result) => {
    if (result) {
      this.designations.push(result); // Add the new Qualification
    }
  }).catch(() => { });
}

saveExperience() {
  if (this.selectedDesignationIndex !== null) {
    debugger;
    // Update the existing Qualification
    this.designations[this.selectedDesignationIndex] = this.designationForm.value;
  } else {
    // If no Qualification is selected, add a new one
    let newDesignation: UserDesignation = {
      userDesignationId: 0,
      designationId: 0,
      userId: this.selectedUserId,
      Name: this.designationForm.value.expDesignation,
      experience: this.designationForm.value.experience,
      isCurrent: this.designationForm.value.isCurrent,
      isActive: false,
      createdById: 0,
      createdDate: new Date().toISOString(), // Current date in ISO format
      modifiedById: 0,
      modifiedDate: new Date().toISOString(),
    };
    
    this.designations.push(newDesignation);
  }
  
  this.modalService.dismissAll(); // Close the modal
  this.designationForm.reset(); // Reset the form
  this.selectedDesignationIndex = null; // Clear the selection
}

// Open Experience Modal for editing
editExperience(index: number) {
  this.selectedDesignationIndex = index;  // Store the index of the Experience being edited
  const Designation = this.designations[index];

  // Set the form values to the clicked Experience data
  this.designationForm.patchValue({
    expDesignationId: Designation.designationId,
    experience: Designation.experience,
    isCurrent: Designation.isCurrent
  });

  // Open the modal
  const modalRef = this.modalService.open(this.editDesignationModal);
  modalRef.result.then((result) => {
    if (result) {
      if (this.selectedDesignationIndex !== null) {
        // Update the Qualification with the edited values
        this.designations[this.selectedDesignationIndex] = this.designationForm.value;
      }
    }
  }).catch(() => {});
}

// Delete Experience Entry
deleteExperience(index: number) {
  this.designations.splice(index, 1);
}

// Helper function to get the name of a degree type from the degreeTypes array
getDeginationName(Id: number) {
  const designation = this.designationTypes.find(d => d.DesignationId == Id);
  return designation ? designation.Name : 'Unknown';
}
//#endregion

//#region Course grid Section
 
 // This function opens the modal for adding or editing courses
 openUserCourseModal(selectedIndex: number | null = null) {
   this.selectedUserCourseIndex = selectedIndex;
   if (selectedIndex !== null) {
     // Pre-fill the form for editing
     const course = this.userCourses[selectedIndex];
     this.userCourseForm.patchValue(course);
   } else {
     // Reset the form for adding new course
     this.userCourseForm.reset();
   }
 
   const modalRef = this.modalService.open(this.editUserCourseModal);
   modalRef.result.then(() => {
     this.saveUserCourse();
   }).catch(() => {});
 }
 
 // Function to save course (either adding new or editing)
 saveUserCourse() {
   if (this.userCourseForm.valid) {
     const newCourse = this.userCourseForm.value;
     if (this.selectedUserCourseIndex !== null) {
       // Edit existing course
       this.userCourses[this.selectedUserCourseIndex] = newCourse;
     } else {
       // Add new course
       this.userCourses.push(newCourse);
     }

     this.modalService.dismissAll(); // Close the modal
     this.userCourseForm.reset();
   }
 }

 editUserCourse(index: number) {
  this.selectedUserCourseIndex = index;
  const course = this.userCourses[index];

  // Set the form values to the selected course's data
  this.userCourseForm.patchValue({
    courseName: course.courseName,
    degreeTypeId: course.degreeTypeId,
    numberOfYearsHandled: course.numberOfYearsHandled,
    isHandledInLast2Semester: course.isHandledInLast2Semester
  });

  // Open the modal for editing
  const modalRef = this.modalService.open(this.editUserCourseModal);
  modalRef.result.then(() => {
    this.saveUserCourse();  // Save the course after modal closes
  }).catch(() => {});  // Handle any modal close error
}

 
 // Function to delete a course
 deleteUserCourse(index: number) {
   this.userCourses.splice(index, 1);
 }
 
 // Helper function to get the name of a degree type from the degreeTypes array
 getDegreeTypeName(degreeTypeId: number) {
   const degree = this.degreeTypes.find(d => d.DegreeTypeId == degreeTypeId);
   return degree ? degree.Name : 'Unknown';
 }
 
 //#endregion


// Submit Form including Specialization & Qualification
  onSubmit() {
    if (this.userForm.valid) {
      const formData = {
        ...this.userForm.value,
        specializations: this.specializations,
        designations: this.designations
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

