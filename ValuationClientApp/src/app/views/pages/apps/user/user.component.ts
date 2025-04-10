import { Component,OnInit, ViewChild } from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal, NgbModalRef  } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { UserService } from '../../services/user.service';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatIconModule } from '@angular/material/icon';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { UserFormComponent } from '../../forms/shared/user-form.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { User  } from '../../models/user.model';
import { ToastrService } from 'ngx-toastr';
import {MatButtonModule} from '@angular/material/button';
import { Router } from '@angular/router';
import { UserProfile } from '../../models/userProfile.model';
import { SpinnerService } from '../../services/spinner.service';

@Component({
  selector: 'app-user',
  imports: [ NgbNavModule,
          NgbDropdownModule,
          NgScrollbarModule,
          NgbTooltip,
          MatTableModule,
          MatPaginatorModule,
          MatSortModule,
          MatIconModule,
          MatLabel,
          MatFormField,
          MatInputModule,
          UserFormComponent,
          MatButtonModule
        ],
  templateUrl: './user.component.html',
  styleUrl: './user.component.scss'
})

export class UserComponent implements OnInit{

  users: any[] = [];
  selectedUser: User | null ;
  modalRef!: NgbModalRef;
  isEditMode:boolean;
  isSubmitting: boolean = false; // Track API call status

  @ViewChild('userModal') userModal: any;

  displayedColumns: string[] = ['name', 'email', 'mobileNumber', 'collegeName', 'isEnabled','actions'];
  dataSource = new MatTableDataSource<any>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(private userService: UserService,
    private modalService: NgbModal,
    private toastr: ToastrService,
    private router: Router,
    private spinnerService: SpinnerService) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    // this.userService.getUsers().subscribe(
    //   (data: any[]) => {
    //     console.log("API Data:", data);  // Debugging step
    //     this.dataSource = new MatTableDataSource(data);
    //     this.dataSource.paginator = this.paginator;
    //     this.dataSource.sort = this.sort;
    //   },
    //   (error) => {
    //     console.error("Error fetching users:", error);
    //   }
    // );

    // this.userService.getUsers().subscribe(response => {
    //   if (response && response.$values) {
    //     this.dataSource.data = response.$values;  // ✅ Extract $values array and assign it to dataSource
    //   } else {
    //     this.dataSource.data = [];  // ✅ Handle empty response case
    //   }
    // }, error => {
    //   console.error('Error fetching users:', error);
    // });


    this.userService.getUsers().subscribe(
      (data: any[]) => {
        console.log('API Data:', data);
        this.users = data;
        this.dataSource.data = this.users; // Avoid reassigning MatTableDataSource
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
      },
      (error) => {
        console.error('Error fetching users:', error);
        this.toastr.error('Failed to load users.');
      }
    );

  }

  editUser(userId: any) {
    this.isEditMode = true; // Set edit mode
    this.router.navigate(['/dashboard/edit', userId]);
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  openCreateUserDialog() {
  // Ensure it's empty for new user
  this.isEditMode = false;
    this.selectedUser = null;
    this.modalRef = this.modalService.open(this.userModal, { size: 'lg', backdrop: 'static' });
  }

  openEditUserDialog(user: any) {
    this.selectedUser = { ...user }; // Load selected user data
    this.isEditMode = true;
    this.modalRef = this.modalService.open(this.userModal, { size: 'lg', backdrop: 'static' });
  }

  handleFormSubmit(userData: any) {
    if (this.isEditMode) {
      // Update logic
      // Merge only changed values into selectedUser
     const updatedUser = { ...this.selectedUser, ...userData };
      this.updateUser(updatedUser);
      console.log('Updating User:', updatedUser);
    } else {
      // Create logic
      this.createUser(userData);
    }
  }

  createUser(userData: any) {
    this.spinnerService.toggleSpinnerState(true);
    this.isSubmitting = true;
    const userObj: UserProfile = {

          userId: 0,
          gender:  '',
          salutation: '',
          name: userData.name || '',
          email: userData.email || '',          
          mobileNumber:userData.mobileNumber.toString() || '',
          departmentName: '',
          collegeName: '',
          roleId: 2,
          mode:'',
          totalExperience: 0,          
          bankAccountName: '',
          bankName : '',
          bankAccountNumber: '',
          bankBranchName: '',
          bankIFSCCode: '',
          isEnabled: true,
          userCourses: [],
          userAreaOfSpecializations: [],
          userQualifications: [],
          userDesignations:[],
          isActive: true,
          createdById:  1,
          createdDate: new Date().toISOString(),
          modifiedById: 1,
          modifiedDate:'',
        };

      console.log('userObj', JSON.stringify(userObj));
      this.userService.addUser(userObj).subscribe({
      next: () => {
        this.toastr.success('User added successfully!');
        this.loadUsers();
        this.modalRef.close();
        this.spinnerService.toggleSpinnerState(false);
      },
      error: (res) => {
        this.toastr.error(res['error']['message']);
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.isSubmitting = false;
        this.spinnerService.toggleSpinnerState(false);
      }
    });
  }

  updateUser(userData:any) {
    if (!this.selectedUser) return;

    this.isSubmitting = true;

    this.userService.updateUser(userData.id.toString(), userData).subscribe({
      next: () => {
        this.toastr.success('User updated successfully!');
        this.loadUsers();
        this.modalRef.close();
      },
      error: (res) => {
        this.toastr.error(res['error']['message']);
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.isSubmitting = false;
        this.spinnerService.toggleSpinnerState(false);
      }
    });
  }

  closeModal() {
    if (this.modalRef) {
      this.modalRef.close();
    }
  }

}
