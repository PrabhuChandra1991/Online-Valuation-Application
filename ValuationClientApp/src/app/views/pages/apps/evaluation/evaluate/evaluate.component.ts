import { NgClass, NgFor } from '@angular/common';
import { NgbModalRef, NgbModal  } from '@ng-bootstrap/ng-bootstrap';
import { OnInit, Component, ViewChild  } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { ToastrService } from 'ngx-toastr';
import { EvaluationService } from '../../../services/evaluation.service';
import { encode, decode } from 'js-base64';
import { ActivatedRoute } from '@angular/router'
import { AnswersheetMark } from '../../../models/answersheetMark.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-evaluate',
  imports: [NgClass, NgFor, CommonModule, PdfViewerModule],
  templateUrl: './evaluate.component.html',
  styleUrl: './evaluate.component.scss'
})

export class EvaluateComponent implements OnInit {
  modalRef: NgbModalRef;
  loggedinUserId: number = 0;
  answersheetId: number = 0;
  primaryData: any;
  answersheetMarkData: any[] = [];
  qaList: any[] = [];
  partList: any[] = [];
  groupList: any[] = [];
  totalMarks: number = 0;
  obtainedMarks: number = 0;
  activeQuestion: string = '---------- Please select the question above to load here ----------';
  activeQuestionImg: string = '';
  activeAnswerKey: string = '---------- Answerkey loads here ----------';
  answersheet: string = '---------- Answersheet loads here ----------';
  activeQuestionMark: string = '';
  sasToken: string = "sp=r&st=2025-04-23T08:48:04Z&se=2025-04-23T16:48:04Z&sv=2024-11-04&sr=c&sig=M%2F90Dwk7LJjwQPE%2FTsYmGnIKl1gpgk%2Fvtbp63LNU5qs%3D"
  answersheetMark: AnswersheetMark;

    @ViewChild('confirmModule') confirmModule: any;

  constructor(
    private modalService: NgbModal,
    private route: ActivatedRoute,
    private toastr: ToastrService,
    private evaluationService: EvaluationService,
    private router: Router
  ) { }

  ngOnInit() {
    console.log("ngOnInit.................");

    const loggedInUser = localStorage.getItem('userData');
    if (loggedInUser) {
      const userData = JSON.parse(loggedInUser);
      this.loggedinUserId = userData.userId;
      console.log("loggedin user id: ", this.loggedinUserId)
    }

    this.route.paramMap.subscribe(async params => {
      let encodedId = params.get('id') || "";
      if (encodedId) {
        this.answersheetId = JSON.parse(decode(encodedId));
        console.log("answersheet id: ", this.answersheetId)
        if (this.answersheetId) {
          await this.getPrimaryData();
          await this.getAnswersheetMark();
          await this.getQuestionPaperAnswerKey();
        }
      }
    });

  }

  async getPrimaryData() {
    //console.log('loading primary data............');
    await this.evaluationService.getAnswerSheetDetails(0, this.answersheetId).subscribe(
      (data: any) => {
        if (data[0]) {
          console.log("primary data: ", data[0])
          this.primaryData = data[0];
          this.answersheet = `${this.primaryData.uploadedBlobStorageUrl}`; //?${this.sasToken}
          this.obtainedMarks = this.primaryData.totalObtainedMark;
        }
        else {
          console.log('No answersheet data');
        }
      },
      (error) => {
        console.error('Error getting answersheet data:', error);
        this.toastr.error('Failed to get answersheet data.');
      }
    )
  }

  async getAnswersheetMark() {
    //console.log('loading saved mark data............');
    await this.evaluationService.getAnswersheetMark(this.answersheetId).subscribe(
      (data: any) => {
        if (data.length > 0) {
          console.log("saved mark data: ", data)
          this.answersheetMarkData = data;
        }
        else {
          console.log('No saved mark data');
        }
      },
      (error) => {
        console.error('Error getting mark data:', error);
        this.toastr.error('Failed to get mark data.');
      }
    )
  }

  async getQuestionPaperAnswerKey() {
    //console.log('loading question and answer............');
    await this.evaluationService.getQuestionAndAnswer(this.answersheetId).subscribe(
      (data: any[]) => {
        if (data.length > 0) {

          data.forEach((item) => {
            this.qaList.push({
              "questionNumber": item.questionNumber,
              "questionPart": item.questionPartName,
              "questionGroup": item.questionGroupName,
              "questionNumberDisplay": item.questionNumberDisplay,
              "questionNumberSubNum": item.questionNumberSubNum,
              "questionDescription": decode(item.questionDescription),
              "questionImage": decode(item.questionImage),
              "answerDescription": decode(item.answerDescription),
              "mark": this.getMark(item.questionMark),
              "obtainedMark": this.savedMarks(item.questionNumber, item.questionNumberSubNum)
            });
          });

          console.log("qaList: ", this.qaList);

          this.getPartList(data);
          //this.getGroupList(data);

          //this.obtainedMarks = this.qaList.reduce((sum, item) => sum + (item.obtainedMark || 0), 0);
          this.reduceChoiceQuestionMarks();

        }
        else {
          console.log('No question and answer data');
        }
      },
      (error) => {
        console.error('Error fetching question and answer:', error);
        this.toastr.error('Failed to load question and answer.');
      }
    );
  }

  getPartList(data: any[]) {
    //console.log("getPartList...............");
    const partMap = new Map();
    data.forEach(({ questionPartName }) => {
      partMap.set(questionPartName, (partMap.get(questionPartName) || 0) + 1);
    });
    this.partList = Array.from(partMap, ([part, count]) => ({ part, count }));
    console.log("partList:", this.partList);
  }

  // getGroupList(data: any[]) {
  //   //console.log("getGroupList...............");
  //   const groupMap = new Map();
  //   data.forEach(({ questionGroupName }) => {
  //     groupMap.set(questionGroupName, (groupMap.get(questionGroupName) || 0) + 1);
  //   });
    
  //   console.log(groupMap);
  //   this.groupList = Array.from(groupMap, ([group, count]) => ({ group, count }));
  //     //.filter(({ count }) => count > 1);
  //   console.log("groupList:", this.groupList);
  // }

  savedMarks(questionNumber: number, questionSubNum: number) {
    let questionRow = this.answersheetMarkData.filter(x => x.questionNumber == questionNumber && x.questionNumberSubNum == questionSubNum)[0];
    if (questionRow) {
      //console.log(questionRow.obtainedMark);
      return questionRow.obtainedMark;
    }
    else {
      return 0;
    }
  }

  getMark(questionMark: string) {
    let mark = 0;
    const dom = new DOMParser().parseFromString(decode(questionMark), 'text/html');
    let spans = dom.querySelectorAll('span');

    spans.forEach((span) => {
      let innerHTML = span.innerHTML.replace(/&nbsp;/g, '').trim();
      if ((innerHTML.length > 0)) {
        mark = parseInt(innerHTML);
      }
    });

    this.totalMarks = this.totalMarks + mark;

    return mark;
  }

  reduceChoiceQuestionMarks() {
    // Remove marks if choice question
    this.groupList.forEach((group) => {
      if (group.count == 2) {
        let item = this.qaList.find(e => e.questionGroup === group.group);
        if (item) {
          this.totalMarks = this.totalMarks - item.mark;
          return;
        }
      }
      if (group.count == 4) {
        let item = this.qaList.find(e => e.questionGroup === group.group);
        if (item) {
          this.totalMarks = this.totalMarks - (item.mark * 2);
          return;
        }
      }
    });
  }

  loadQuestionDetails(event: any, id: number, qusetionNo: string) {
    console.log("getting question.................");

    let btnList = document.querySelectorAll(".question .btn");
    let txtList = document.querySelectorAll(".question .form-control");
    let btnid = `#btn${id}`;
    let txtid = `#txt${id}`;
    let btn = document.querySelector(btnid);
    let txt = document.querySelector(txtid);

    btnList.forEach((item) => {
      if (item.classList.contains('active')) {
        item.classList.remove('active');
      }
    });
    txtList.forEach((item) => {
      if (item.classList.contains('active')) {
        item.classList.remove('active');
      }
    });

    btn?.classList.add('active');
    txt?.classList.add('active');

    let matchedItem = this.qaList.filter(x => x.questionNumberDisplay == qusetionNo)[0];

    if (matchedItem) {
      this.activeQuestion = matchedItem.questionDescription;
      this.activeQuestionImg = matchedItem.questionImage;
      this.activeAnswerKey = matchedItem.answerDescription;
      this.activeQuestionMark = matchedItem.mark;
    }

  }

  validateMark(event: any, item: any) {
    if (event.target.value) {
      if (event.target.value.match(/[^0-9.]/g)) {
        this.toastr.error('Please add only numbers.');
        event.target.value = ''; //event.target.value.replace(/[^\d]/g, '');        
        return;
      }
      else if ((parseFloat(event.target.value) > parseInt(event.srcElement.max))) {
        let msg = `Maximum marks allowed is ${event.srcElement.max}`
        this.toastr.error(msg);
        event.target.value = '';
        return;
      }
      else {
        this.saveMark(item, parseFloat(event.target.value));
      }

      // this.obtainedMarks = 0;
      // let txtList = document.querySelectorAll(".question .form-control") as NodeListOf<HTMLInputElement>;
      // txtList.forEach((item: HTMLInputElement) => {
      //   if (item.value) {
      //     this.obtainedMarks += parseFloat(item.value); // sum all text box marks
      //   }
      // });

    }
  }

  saveMark(item: any, obtainedMark: number) {
    this.answersheetMark = {
      "createdById": this.loggedinUserId,
      "modifiedById": this.loggedinUserId,
      "answersheetId": this.primaryData.answersheetId,
      "questionPartName": item.questionPart,
      "questionGroupName": item.questionGroup,
      "questionNumber": item.questionNumber,
      "questionNumberSubNum": item.questionNumberSubNum,
      "maximumMark": item.mark,
      "obtainedMark": obtainedMark
    }

    console.log("answersheetMark: ", this.answersheetMark)

    this.evaluationService.saveAnswersheetMark(this.answersheetMark).subscribe(
      (data: any) => {
        console.log("save data: ", data)
        if (data.message.toLowerCase() == 'success') {
          this.toastr.success("Mark auto saved.");
          this.obtainedMarks = data.totalMarksObtained;
        }
      },
      (error) => {
        console.error('Error saving mark:', error);
        this.toastr.error('Failed to save mark.');
      }
    )
  }

  promptBeforeCompletion() {
      this.modalRef = this.modalService.open(this.confirmModule, { size: 'md', backdrop: 'static' });
    }

  completeEvaluation() {
    // if (confirm("Are you sure you want to complete evaluation? \nYou cannot re-evaluate once submitted.")) {
    //   if (this.obtainedMarks == 0) {
    //     this.toastr.warning('Obained Marks is 0. Please evaluate.');
    //   }
    //   else {
    //     this.evaluationService.completeEvaluation(this.primaryData.answersheetId, this.loggedinUserId).subscribe(
    //       (data: any) => {
    //         console.log("respo", data)
    //         if (data.message.toLowerCase() == 'success') {
    //           this.toastr.success("Evaluation completed successfully");
    //           this.backToList();
    //         }
    //         else {
    //           console.error('Failed to complete evaluation');
    //           this.toastr.error('Failed to complete evaluation.');
    //         }
    //       },
    //       (error) => {
    //         console.error('Error while saving evaluation:', error);
    //         this.toastr.error('Failed to complete evaluation.');
    //       }
    //     )
    //   }
    // } else {
    //   // User clicked Cancel or closed the dialog
    //   // Handle cancellation
    // }    
  }

  backToList() {
    this.router.navigate(['/apps/evaluationlist']);
  }

  filter(part: string) {
    let list = this.qaList.filter(e => e.part === part);
    return list;
  }

  onError(error: any) {
    console.error('PDF error: ', error);
  }

}
