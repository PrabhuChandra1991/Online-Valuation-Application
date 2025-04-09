import { AfterViewInit, Component, ElementRef, OnInit, TemplateRef, viewChild, ViewChild } from '@angular/core';
import { NgbCalendar, NgbDatepickerModule, NgbDateStruct, NgbDropdownModule, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { FormBuilder, FormGroup, FormArray, Validators, FormsModule, ReactiveFormsModule, FormControl } from '@angular/forms';
import { NgApexchartsModule } from 'ng-apexcharts';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { UserService } from '../services/user.service';
import { MatIcon } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import {MatButtonModule} from '@angular/material/button';
import { DashboardService } from '../services/dashboard.service';
import { UserAreaOfSpecialization  } from '../models/userAreaOfSpecialization.model';
import { UserDesignation } from '../models/userDesignation.model';
import {UserProfile} from '../models/userProfile.model';
import { UserQualification } from '../models/userQualification';
import { User } from '../models/user.model';
import { UserCourse } from '../models/userCourse.model';
import { SpinnerService } from '../services/spinner.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

const currentDate = new Date().toISOString();




@Component({
  selector: 'app-dashboard',
  imports: [
    FormsModule,
    ReactiveFormsModule,  // Ensure this is present
    NgbDropdownModule,
    NgbDatepickerModule,
    NgApexchartsModule,
    CommonModule,
    MatIcon,
    MatCheckboxModule,
    MatButtonModule,
    RouterLink,
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})



export class DashboardComponent implements OnInit   {


  @ViewChild('ugNameInput') ugNameInput!: ElementRef;
  @ViewChild('ugSpecializationInput') ugSpecializationInput!: ElementRef;
  @ViewChild('pgNameInput') pgNameInput!: ElementRef;
  @ViewChild('pgSpecializationInput') pgSpecializationInput!: ElementRef;
  @ViewChild('phdSpecializationInput') phdSpecializationInput!: ElementRef;
  @ViewChild('salutation') salutationInput!: ElementRef;
  @ViewChild('gender') genderInput!: ElementRef;


  userForm!: FormGroup;
  designationForm!: FormGroup;
  specializationForm!: FormGroup;
  userCourseForm!: FormGroup;
  qualificationInput!: FormGroup;

  selectedDesignationIndex: number | null = null;
  selectedSpecializationIndex: number | null = null;
  selectedUserCourseIndex: number | null = null;
  selectedUserId:any;
  userQualifications: any[] = [];
  userCourses :any [] = [];
  qualification : UserQualification;
  Salutation:any;
  selectedUser:UserProfile;
  selectedSalutation:any;
  selectedGender:any;
  showCurrentPostion:boolean = true;
  isFormInvalid : boolean = true;
  @ViewChild('editSpecializationModal') editSpecializationModal!: TemplateRef<any>;
  @ViewChild('editDesignationModal') editDesignationModal!: TemplateRef<any>;
  @ViewChild('editUserCourseModal') editUserCourseModal!: TemplateRef<any>;

  isMaxReached: boolean = false;
  maxAllowedCount:number = 10;

  isUserEnabled = false;

  validationErrors: { [key: string]: boolean } = {
    ugName: false,
    ugSpecialization: false,
    pgName: false,
    pgSpecialization: false,
    phdSpecialization:false
  };




  //this should be load from dashboard service
  salutations = [
    { id: 'Mr.', name: 'Mr.' },
    { id: 'Ms.', name: 'Ms.' },
    { id: 'Mrs.', name: 'Mrs.' },
    { id: 'Dr.', name: 'Dr.' },
    { id: 'Prof.', name: 'Prof.' }
  ];


  genders = [
    { id: 'Male', name: 'Male' },
    { id: 'Female', name: 'Female' },
    { id: 'Other', name: 'Other' }
  ];

  degreeTypes = [
    {
      DegreeTypeId: 1,
      Name: 'UG',
      Code: 'UG',
      IsActive: 1,
      CreatedDate: currentDate,
      CreatedById: 1,
      ModifiedDate: currentDate,
      ModifiedById: 1
    },
    {
      DegreeTypeId: 2,
      Name: 'PG',
      Code: 'PG',
      IsActive: 1,
      CreatedDate: currentDate,
      CreatedById: 1,
      ModifiedDate: currentDate,
      ModifiedById: 1
    }
    // {
    //   DegreeTypeId: 3,
    //   Name: 'Ph.D',
    //   Code: 'Ph.D',
    //   IsActive: 1,
    //   CreatedDate: currentDate,
    //   CreatedById: 1,
    //   ModifiedDate: currentDate,
    //   ModifiedById: 1
    // }
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
    userId: this.selectedUserId,
    experience: 0,
    isCurrent: false
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
    private dashboardService: DashboardService,
    private spinnerService: SpinnerService,
    private router: Router
  ) {

     // Initialize the form with validation
     this.userCourseForm = this.fb.group({
      courseName: ['', Validators.required],
      degreeTypeId: ['', Validators.required],
      numberOfYearsHandled: ['', [Validators.required, Validators.min(1)]],
      isHandledInLast2Semester: [false]
    });

    // Initialize the form with default values
    this.specializationForm = this.fb.group({
      areaOfSpecializationName: ['', Validators.required],
    }),

    this.designationForm = this.fb.group({
      expName: ['', Validators.required],
      experience: ['', [Validators.required, Validators.min(1)]],
      isCurrent: [false]
    });

    this.userForm = this.fb.group({
      genderId: ['', Validators.required],
      name:  ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      mobileNumber: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
      departmentName: ['', Validators.required],
      designationId: ['', [Validators.required],Validators.pattern('^[a-zA-Z \-\']+')],
      collegeName: ['', Validators.required],
      bankAccountName: ['', Validators.required],
      bankAccountNumber: ['', [Validators.required, Validators.pattern('^[0-9]+$')]],
      bankBranchName: ['', Validators.required],
      bankName:['', Validators.required],
      bankIFSCCode: ['', Validators.required],
      specializations: this.fb.array([]),
      designations: this.fb.array([]),

      // qualificationInput: this.fb.group({
      //   ugName: ['', Validators.required],
      //   ugSpecialization: ['']
      // }),

      ugName: ['', Validators.required],
      ugSpecialization: ['', [Validators.required]],
      pgName: ['', [Validators.required]],
      pgSpecialization: ['', [Validators.required]],
      phdSpecialization: ['', [Validators.required]],
      hasPhd: [false], // Checkbox for PhD completion,
      salutationId:['', Validators.required],
      department:['', Validators.required],
      mode:['']
    });
  }

  ngOnInit(): void {
    console.log('des',this.designations);
    this.route.paramMap.subscribe(params => {
      const userId = params.get('id'); // Get user ID from route
      if (userId) {
        this.selectedUserId = userId;
        this.getUser(userId); // Load user data for editing
      }
      else{
        this.loadUserData();
      }
    });

    //getting master data
    this.degreeTypes = this.getDegreeTypes();
    this.designations = this.getdesignations();


    this.userForm.controls['name'].valueChanges.subscribe(value => {
      if (value) {
        this.userForm.controls['bankAccountName'].setValue(value);
        this.userForm.controls['bankAccountName'].disable();
      }
    });

  }

  onInput(field: string, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.validationErrors[field] = value.trim() === '';
  }

  onBlur(controlName: string): void {

    const inputMap: { [key: string]: ElementRef | undefined } = {
      ugName: this.ugNameInput,
      pgName: this.pgNameInput,
      ugSpecialization: this.ugSpecializationInput,
      pgSpecialization: this.pgSpecializationInput,
      phdSpecialization: this.phdSpecializationInput
    };

    const inputRef = inputMap[controlName];

    if (inputRef && inputRef.nativeElement) {
      const value = inputRef.nativeElement.value?.trim();
      this.validationErrors[controlName] = value === '';
    }
  }

  validateInputs(): void {
    const inputsToValidate = [
      { key: 'ugName', ref: this.ugNameInput },
      { key: 'pgName', ref: this.pgNameInput },
      { key: 'ugSpecialization', ref: this.ugSpecializationInput },
      { key: 'pgSpecialization', ref: this.pgSpecializationInput },
      { key: 'phdSpecialization', ref: this.phdSpecializationInput }
    ];

    inputsToValidate.forEach(input => {
      if (input.ref && input.ref.nativeElement) {
        const value = input?.ref?.nativeElement?.value?.trim();
        this.validationErrors[input.key] = !value;
      }
    });
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

      if(userData.isEnabled)
        this.isUserEnabled = true;
        //this.userForm.controls['name'].disable();


      console.log("localstoreUserData",userData);
    }
  }

  getUser(userId:any)
  {
    this.userService.getUser(userId).subscribe({
      next: (data) => {
       let selectedUser = data;
       console.log('user Data',data );

       if(selectedUser)
       {
        this.selectedUser = selectedUser;

        //assign child elements dynamically
        this.designations = selectedUser.userDesignations;
        this.specializations = selectedUser.userAreaOfSpecializations;
        this.userQualifications = selectedUser.userQualifications;
        this.userCourses = selectedUser.userCourses;

        //filter data for qualification section
        let ugQualification = selectedUser.userQualifications.find((f: { title: string }) => f.title.includes('UG'));
        let pgQualification = selectedUser.userQualifications.find((f: { title: string }) => f.title.includes('PG'));
        let phdQualification = selectedUser.userQualifications.find((f: { title: string }) => f.title.includes('Ph'));

        let salutation = this.salutations.find(f => f.name == selectedUser.salutation);
        let gender = this.genders.find(f => f.name == selectedUser.gender);

        this.salutationInput.nativeElement.value = salutation?.id;
        this.genderInput.nativeElement.value = gender?.id;

          console.log('gender',gender);
          console.log("salutation",salutation);
          console.log("user",selectedUser);
          console.log("ug",ugQualification);
          console.log("pg",pgQualification);
          console.log("phd",phdQualification);
          console.log("before patch",this.userForm.value);

          this.userForm.patchValue({

            salutationId: salutation?.id,
            genderId: gender?.id,
            name: selectedUser?.name,
            email: selectedUser?.email,
            mobileNumber: Number(selectedUser?.mobileNumber),
            collegeName: selectedUser?.collegeName,
            ugName: ugQualification?ugQualification.name:'',
            departmentName:selectedUser?.departmentName,
            ugSpecialization: ugQualification?ugQualification.specialization:'',
            pgName:pgQualification?pgQualification.name:'',
            pgSpecialization: pgQualification?pgQualification.specialization:'',
            bankAccountName: selectedUser?.name,
            bankAccountNumber: selectedUser.bankAccountNumber,
            bankBranchName: selectedUser?.bankBranchName,
            bankName:selectedUser.bankName,
            bankIFSCCode: selectedUser.bankIFSCCode,
            department:selectedUser.departmentName,
            hasPhd : phdQualification?phdQualification.isCompleted:false,
            phdSpecialization:phdQualification?phdQualification.specialization:'',
            userId:selectedUser?.userId,
            mode:selectedUser?.mode

        });

        this.userForm.updateValueAndValidity();

        if(selectedUser.isEnabled)
          this.isUserEnabled = true;
          //this.userForm.controls['name'].disable();

        console.log("after patch",this.userForm.value);


       }
        console.log('selectedUser Details:', selectedUser);
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

    const isDuplicate = this.specializations.some((specilization , index)=>
      index !== this.selectedSpecializationIndex &&
      specilization.areaOfSpecializationName.trim().toLowerCase() === this.specializationForm.value.areaOfSpecializationName.trim().toLowerCase()
    );

    if (isDuplicate) {
      this.toastr.error('This area of specialization already exists.');
      return;
    }

    if (this.selectedSpecializationIndex !== null) {
      // Update the existing specialization
      this.specializations[this.selectedSpecializationIndex].areaOfSpecializationName = this.specializationForm.value.areaOfSpecializationName;
    this.selectedSpecializationIndex = 0; // Clear the selection
    } else {

      if (this.specializations.length >= 3) {
        this.toastr.error('Maximum of 3 area of specializations allowed.');
        return;
      }



      // If no specialization is selected, add a new one
      let newSpecialization: UserAreaOfSpecialization = {
        userAreaOfSpecializationId: 0,
        userId: this.selectedUserId,
        areaOfSpecializationName: this.specializationForm.value.areaOfSpecializationName,
        isActive: true,
        createdDate: currentDate,
        createdById: this.selectedUserId,
        modifiedDate: currentDate,
        modifiedById: this.selectedUserId
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
  //set isCurrent
  this.showCurrentPostion = !this.designations.some(f => f.isCurrent === true);

  const modalRef = this.modalService.open(this.editDesignationModal);
  modalRef.result.then((result) => {
    if (result) {
      this.designations.push(result); // Add the new Qualification
    }
  }).catch(() => { });
}

saveExperience() {

  let experienceFormValue = this?.designationForm?.value;

  const isDuplicate = this.designations.some((exp, index) =>
    index !== this.selectedDesignationIndex &&
    exp.designationId == experienceFormValue.expName &&
    exp.experience == experienceFormValue.experience
    //exp.isCurrent === (experienceFormValue.isCurrent ? true : false)
  );

  if (isDuplicate) {
    this.toastr.error('This experience already exists.');
    return;
  }

  if (this.selectedDesignationIndex !== null) {

    // Update the existing Qualification
    this.designations[this.selectedDesignationIndex].designationId = this.designationForm.value.expName;
    this.designations[this.selectedDesignationIndex].experience = this.designationForm.value.experience;
    this.designations[this.selectedDesignationIndex].isCurrent = this.designationForm.value.isCurrent;

  } else {


    if (this.designations.length >= 3) {
      this.toastr.error('Maximum of 3 experience allowed.');
      return;
    }

    // const isDuplicate = this.designations.some(exp =>
    //   exp.designationId === experienceFormValue.expName &&
    //   exp.experience ===  experienceFormValue.experience)
    //   // exp.isCurrent ===  experienceFormValue.isCurrent ? true : false)

    // if (isDuplicate) {
    //   this.toastr.error('This experience already exists.')
    //   return;
    // }

    // If no Qualification is selected, add a new one
    let newDesignation: UserDesignation = {
      userDesignationId:0,
      designationId: this.designationForm.value.expName,
      userId: this.selectedUserId,
      experience: this.designationForm.value.experience,
      isCurrent: this.designationForm?.value?.isCurrent?this.designationForm?.value?.isCurrent: false,
      isActive: true,
      createdById: this.selectedUserId,
      createdDate: currentDate,
      modifiedById: this.selectedUserId,
      modifiedDate: currentDate
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

  this.showCurrentPostion = Designation.isCurrent || !this.designations.some(f => f.isCurrent === true);

  // Set the form values to the clicked Experience data
  this.designationForm.patchValue({
    expName: Designation.designationId,
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
getDesignationName(Id: number) {
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

    const isDuplicate = this.userCourses.some((course, index) =>
     index !== this.selectedUserCourseIndex &&
     course.courseName.trim().toLowerCase() == newCourse.courseName.trim().toLowerCase() &&
     course.degreeTypeId == newCourse.degreeTypeId &&
     course.numberOfYearsHandled == newCourse.numberOfYearsHandled &&
     course.isHandledInLast2Semester == (newCourse.isHandledInLast2Semester ? true : false)
   );

   if (isDuplicate) {
     this.toastr.error('This course already exists.')
     return;
   }


    if (this.selectedUserCourseIndex !== null) {
      // Edit existing course
      this.userCourses[this.selectedUserCourseIndex].courseName = newCourse.courseName;
      this.userCourses[this.selectedUserCourseIndex].degreeTypeId = newCourse.degreeTypeId;
      this.userCourses[this.selectedUserCourseIndex].numberOfYearsHandled = newCourse.numberOfYearsHandled;
      this.userCourses[this.selectedUserCourseIndex].isHandledInLast2Semester = newCourse.isHandledInLast2Semester;
    } else {

     if (this.userCourses.length >= 15) {
       this.toastr.error('Maximum of 15 courses allowed.');
       return;
     }

     // const isDuplicate = this.userCourses.some(course =>
     //   course.courseName.trim().toLowerCase() == newCourse.courseName.trim().toLowerCase() &&
     //   course.degreeTypeId == newCourse.degreeTypeId &&
     //   course.numberOfYearsHandled == newCourse.numberOfYearsHandled &&
     //   course.isHandledInLast2Semester == (newCourse.isHandledInLast2Semester ? true : false)
     // );

     // if (isDuplicate) {
     //   this.toastr.error('This course already exists.')
     //   return;
     // }

      // Add new course
      let newCourseObj: UserCourse = {
        userCourseId: 0,
        userId: this.selectedUserId,
        courseName: newCourse.courseName,
        degreeTypeId: newCourse.degreeTypeId,
        numberOfYearsHandled: newCourse.numberOfYearsHandled,
        isHandledInLast2Semester: newCourse.isHandledInLast2Semester?newCourse.isHandledInLast2Semester:false ,
        isActive: true,
        createdById: this.selectedUserId,
        createdDate: currentDate,
        modifiedById: this.selectedUserId,
        modifiedDate: currentDate
      }
      this.userCourses.push(newCourseObj);
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

  OnPhDCheck() {
    let data = this.userQualifications.find(f => f.title.includes('Ph.D'));
    if(data)
      data.isCompleted = !data.isCompleted;
  }

 //#endregion

 onSalutationChange(event: Event) {
  const selectedValue = (event.target as HTMLSelectElement).value;
  this.userForm.patchValue({ salutationId: selectedValue });
}

onGenderChange(event: Event) {
  const selectedValue = (event.target as HTMLSelectElement).value;
  this.userForm.patchValue({ genderId: selectedValue });
}

isCustomFormInvalid(): boolean {
  const excludedFields = new Set([
    'ugName',
    'ugSpecialization',
    'pgName',
    'pgSpecialization',
    'phdSpecialization',
    'designationId',
    'department'
  ]);

  let isInvalid = false;

  Object.keys(this.userForm.controls).forEach(controlName => {
    if (!excludedFields.has(controlName)) {
      const control = this.userForm.get(controlName);
      if (control && control.invalid) {
        isInvalid = true;
      }
    }
  });

  return isInvalid;
}

validateTableData(): boolean {
  const errorMessages: string[] = [];

  if (!this.specializations?.length) {
    errorMessages.push('Please add at least one area of specialization.');
  }

  if (!this.designations?.length) {
    errorMessages.push('Please add at least one designation experience.');
  }

  if (!this.userCourses?.length) {
    errorMessages.push('Please add at least one user course(s).');
  }

  if (errorMessages.length > 0) {
    const formattedMessage = errorMessages.length === 1
      ? errorMessages[0]
      : errorMessages.map((msg, index) => `${index + 1}. ${msg}`).join('<br/>');

    this.toastr.error(formattedMessage, 'Table Data Error', {
      enableHtml: true,
    });

    return false;
  }

  return true;
}


// Submit Form including Specialization & Qualification
  onSubmit() {

    this.validateInputs();

    const isCustomInvalid = this.isCustomFormInvalid();
    const isTableDataInvalid = !this.validateTableData();

    const isFormChanged = this.userForm.dirty;
    const isFormInvalid = isCustomInvalid;

    if (isFormInvalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    if (isTableDataInvalid) {
      return;
    }

    // Proceed with submission
    this.spinnerService.toggleSpinnerState(true);

    const formData = {
      ...this.userForm.value,
      specializations: this.specializations,
      designations: this.designations,
      courses: this.userCourses,
      userQualifications: this.userQualifications
    };

    const userData = this.mapFormDataToUserObject(formData);
    //userData.isEnabled = true;

    this.updateUser(userData);
    console.log('Final Object:', JSON.stringify(userData));
  }

   mapFormDataToUserObject(formData: any) {

    //map course details

     let ugCourse = this.userQualifications.find(f => f.title.includes('UG'));
     let pgCourse = this.userQualifications.find(f => f.title.includes('PG'));
     let phdSCourse = this.userQualifications.find(f => f.title.includes('Ph.D'));

     if (ugCourse) {
       ugCourse.name =  this.ugNameInput.nativeElement.value;
       ugCourse.specialization = this.ugSpecializationInput.nativeElement.value;
     }

     if (ugCourse) {
       pgCourse.name = this.pgNameInput.nativeElement.value;;
       pgCourse.specialization = this.pgSpecializationInput.nativeElement.value;
     }

     if (phdSCourse) {
      if(this.phdSpecializationInput?.nativeElement?.value)
        phdSCourse.specialization = this.phdSpecializationInput.nativeElement.value;
    }



    const userObj: UserProfile = {

      userId: this.selectedUserId || 0,
      name: formData.name || '',
      email: formData.email || '',
      gender: formData.genderId || '',
      salutation: formData.salutationId || '',
      mobileNumber: formData.mobileNumber || '',
      roleId:  this.selectedUser?.roleId || 0,
      mode: this.selectedUser?.mode || '',
      totalExperience: formData.totalExperience || 0,
      departmentName: formData.departmentName || '',
      collegeName: formData.collegeName || '',
      bankAccountName: formData.name  || '',
      bankName : formData.bankName || '',
      bankAccountNumber: formData.bankAccountNumber || '',
      bankBranchName: formData.bankBranchName || '',
      bankIFSCCode: formData.bankIFSCCode || '',
      isEnabled: true,
      userCourses: formData.courses || [],
      userAreaOfSpecializations: formData.specializations || [],
      userQualifications: this.userQualifications || [],
      userDesignations: formData.designations || [],
      isActive: true,
      createdById: this.selectedUser.createdById || 0,
      createdDate: this.selectedUser.createdDate || '',
      modifiedById: this.selectedUserId || 0,
      modifiedDate: currentDate || ''
    };

    return userObj;
  }

  getQualification(formData: any): UserQualification {

    const userQualification: UserQualification = {

      userQualificationId: formData.userQualificationId,
      userId: formData.userId,
      title: formData.title,
      name: formData.name,
      specialization: formData.specialization,
      isCompleted: formData?.isCompleted?formData.isCompleted:false,
      isActive: true,
      createdById: formData.createdById,
      createdDate: formData.createdDate,
      modifiedById: formData.userId,
      modifiedDate: formData.modifiedDate
    };



    return userQualification;
  }

  updateUser(userData: any) {
    this.userService.updateUser(userData.userId, userData).subscribe({
      next: () => {
        this.toastr.success('User updated successfully!');
        let  loggedUserData = null;
      
         const userDataString = localStorage.getItem('userData');
         loggedUserData = userDataString ? JSON.parse(userDataString) : null;
        
         if (localStorage.getItem('isLoggedin') === 'true') {
          if(loggedUserData.roleId == 1)
            this.router.navigate(['/apps/user']);
          else
            this.router.navigate(['/apps/assigntemplate']);
           }
      },
      error: () => {
        this.toastr.error('Failed to update user. Please try again.');
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.spinnerService.toggleSpinnerState(false);
       }
    });
  }
}





