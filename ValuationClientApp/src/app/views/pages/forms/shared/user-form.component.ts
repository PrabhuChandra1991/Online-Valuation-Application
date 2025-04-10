import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports:[ReactiveFormsModule,CommonModule],
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.css']
})
export class UserFormComponent {
  @Input() userData: any = null; // Data for edit mode
  @Input() isEditMode: boolean = false; // Flag for edit mode
  @Output() formSubmit = new EventEmitter<any>();
  @Output() closeModal = new EventEmitter<void>();

  userForm: FormGroup;
  isSubmitting = false;

  constructor(private fb: FormBuilder,private toastr: ToastrService) {
    this.userForm = this.fb.group({
      name: [{ value: '', disabled: this.isEditMode }, Validators.required],
      email: [{ value: '', disabled: this.isEditMode }, [Validators.required, Validators.email]],
      mobileNumber: [{ value: '', disabled: this.isEditMode }, [Validators.required, Validators.pattern(/^[0-9]{10}$/)]],
      collegeName: [''],
      departmentName: [''],
      qualification: [''],
      workExperience: [0, [Validators.min(0)]]      
    });
  }

  ngOnInit() {
    if (this.userData) {
      this.userForm.patchValue(this.userData);
    }

    // Disable mandatory fields if in edit mode
    if (this.isEditMode) {
      this.userForm.controls['name'].disable();
      this.userForm.controls['email'].disable();
      this.userForm.controls['mobileNumber'].disable();
    }
  }

  save() {
    if (this.userForm.invalid) return; // Prevent submission if invalid

    this.isSubmitting = true;
    //this.formSubmit.emit(this.userForm.value); // Emit valid form data
    this.formSubmit.emit(this.userForm.getRawValue()); // Get disabled values to
  }

  close() {
    this.closeModal.emit();
  }
}
