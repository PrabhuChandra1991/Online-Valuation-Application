import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
  
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { FormGroup, FormControl, Validators } from '@angular/forms';
  
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import { SpinnerService } from '../../services/spinner.service';
import { ImportService } from '../../services/import.service';

@Component({
  selector: 'app-master',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, HttpClientModule],
  templateUrl: './master.component.html',
  styleUrl: './master.component.scss'
})
export class MasterComponent {

  isSubmitting = false;
  fileName = '';

  masterForm = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    file: new FormControl('', [Validators.required]),
    fileSource: new FormControl('', [Validators.required])
  });

  constructor(private toastr: ToastrService,private ImportService: ImportService,private spinnerService : SpinnerService) {
    
  }

  get f(){
    return this.masterForm.controls;
  }

onFileChange(event:any) {
    debugger;
    if (event.target.files.length > 0) {
      const file = event.target.files[0];
      this.masterForm.patchValue({
        fileSource: file
      });
    }
  } 

  submit(){
    debugger;

    this.spinnerService.toggleSpinnerState(true);
    const formData = new FormData();
  
    const fileSourceValue = this.masterForm.get('fileSource')?.value;
  
    if (fileSourceValue !== null && fileSourceValue !== undefined) {
        formData.append('file', fileSourceValue);
    }
       
    this.ImportService.importData(formData)
    .subscribe({
      next: () => {
        this.toastr.success('Data imported successfully!');

      
      },
      error: () => {
        this.toastr.error('Failed to import data. Please try again.');
        this.spinnerService.toggleSpinnerState(false);
      },
      complete: () => {
        this.spinnerService.toggleSpinnerState(false);
       }
    });
      
  }
}
