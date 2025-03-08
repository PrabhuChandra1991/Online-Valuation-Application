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
import {MatButtonModule} from '@angular/material/button';

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
  selectedUser: any;
  modalRef!: NgbModalRef;
  isEditMode:boolean;

  @ViewChild('userModal') userModal: any;

  displayedColumns: string[] = ['name', 'email', 'mobileNumber', 'collegeName', 'isEnabled','actions'];
  dataSource = new MatTableDataSource<any>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(private userService: UserService,private modalService: NgbModal) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.userService.getUsers().subscribe(
      (data: any[]) => {
        console.log("API Data:", data);  // Debugging step 
        this.dataSource = new MatTableDataSource(data);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
      },
      (error) => {
        console.error("Error fetching users:", error);
      }
    );
  }

  editUser(user: any) {
    this.selectedUser = { ...user }; // Store selected user details
    this.isEditMode = true; // Set edit mode
  }
  

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }


  openCreateUserDialog() {
    this.selectedUser = null; // Ensure it's empty for new user
    this.modalRef = this.modalService.open(this.userModal, { size: 'lg', backdrop: 'static' });
  }

  openEditUserDialog(user: any) {
    this.selectedUser = { ...user }; // Load selected user data
    this.modalRef = this.modalService.open(this.userModal, { size: 'lg', backdrop: 'static' });
  }

  handleFormSubmit(userData: any) {
    if (this.selectedUser) {
      // Update logic
      this.updateUser(userData);
      console.log('Updating User:', userData);
    } else {
      // Create logic
      this.selectedUser = {
        id: 0,
        name:  userData.name,
        email: userData.email,
        mobileNumber: userData.mobileNumber,
        roleId: null,
        workExperience: null,
        departmentId: null,
        designationId: null,
        collegeName: userData.collegeName,
        bankAccountName: null,
        bankAccountNumber: null,
        bankName: null,
        bankBranchName: null,
        isEnabled: false,
        qualification: userData.qualification,
        areaOfSpecialization: null,
        courseId: 0,
        bankIfsccode: null,
        isActive: false,
        createdById: 0,
        createdDate: new Date().toISOString(),
        modifiedById: 0,
        modifiedDate: null
      };
    
    this.addUser();
      console.log('Creating User:', this.selectedUser);
      this.modalRef.close();
    }
   
  }

  // Add a new user
  addUser() {
    this.userService.addUser(this.selectedUser).subscribe(() => {
   
      this.modalService.dismissAll(); // Close modal after adding
      this.loadUsers(); // Refresh user list
    });
  }

  updateUser(userData:any) {
    this.userService.updateUser(this.selectedUser.id, this.selectedUser).subscribe(() => {
      this.modalService.dismissAll(); // Close modal after updating
      this.loadUsers(); // Refresh user list
    });
  }

  closeModal() {
    this.modalRef.close();
  }

}



