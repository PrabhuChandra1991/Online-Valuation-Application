import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-user-form',
  imports:[ReactiveFormsModule],
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.css']
})
export class UserFormComponent {
  @Input() userData: any = null; // Data for edit mode
  @Output() formSubmit = new EventEmitter<any>();
  @Output() closeModal = new EventEmitter<void>();

  userForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.userForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      mobileNumber: ['', Validators.required],
      qualification: [''],
      workExperience: [''],
      collegeName: ['']
    });
  }

  ngOnInit() {
    if (this.userData) {
      this.userForm.patchValue(this.userData);
    }
  }

  save() {
    if (this.userForm.valid) {
      this.formSubmit.emit(this.userForm.value);
    }
  }

  close() {
    this.closeModal.emit();
  }
}
