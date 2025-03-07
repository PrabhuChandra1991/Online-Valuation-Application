import { Component,OnInit, ViewChild } from '@angular/core';
import { NgbDropdownModule, NgbNavModule, NgbTooltip, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { UserService } from '../../services/user.service';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatIconModule } from '@angular/material/icon';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';


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
          MatInputModule
        ],
  templateUrl: './user.component.html',
  styleUrl: './user.component.scss'
})
export class UserComponent implements OnInit{

  users: any[] = [];

  displayedColumns: string[] = ['name', 'email', 'mobileNumber',  'collegeName', 'isEnabled'];
  dataSource = new MatTableDataSource<any>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(private userService: UserService,private router: Router, private route: ActivatedRoute) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.userService.getUsers().subscribe(
      (data: any[]) => {
        console.log("API Data:", data);  // Debugging step ✅
        this.dataSource = new MatTableDataSource(data);
        this.dataSource.paginator = this.paginator;
        this.dataSource.sort = this.sort;
      },
      (error) => {
        console.error("Error fetching users:", error);
      }
    );
  }

  createUser(){
    
    this.router.navigate(['/auth/register']);
    
  }
  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

}


