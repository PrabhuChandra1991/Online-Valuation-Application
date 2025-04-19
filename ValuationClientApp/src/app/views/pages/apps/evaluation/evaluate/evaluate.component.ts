import { NgClass, NgFor } from '@angular/common';
import { OnInit, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { ToastrService } from 'ngx-toastr';
import { EvaluationService } from '../../../services/evaluation.service';
import { encode, decode } from 'js-base64';
import { ActivatedRoute } from '@angular/router'
import { AnswersheetMark } from '../../../models/answersheetMark.model';

@Component({
  selector: 'app-evaluate',
  imports: [NgClass, NgFor, CommonModule, PdfViewerModule],
  templateUrl: './evaluate.component.html',
  styleUrl: './evaluate.component.scss'
})

export class EvaluateComponent implements OnInit {
  loggedinUserId: number = 0;
  primaryData: any;
  answersheetMarkData: any[] = []; // pull if Marks entered already
  qaList: any[] = [];
  partList: any[] = [];
  groupList: any[] = [];
  totalMarks: number = 0;
  obtainedMarks: number = 0;
  activeQuestion: string = '---------- Please select the question above to load here ----------';
  activeQuestionImg: string = '';
  activeAnswerKey: string = '---------- Answerkey loads here ----------';
  activeAnswersheet: string = '---------- Answersheet loads here ----------';
  activeQuestionMark: string = '';

  answersheetMark: AnswersheetMark;
  src = "https://vadimdez.github.io/ng2-pdf-viewer/assets/pdf-test.pdf";
  //src = 'https://iris.who.int/bitstream/handle/10665/137592/roadmapsitrep_7Nov2014_eng.pdf';

  constructor(
    private route: ActivatedRoute,
    private toastr: ToastrService,
    private evaluationService: EvaluationService
  ) { }

  ngOnInit(): void {
    console.log("ngOnInit.................");

    const loggedInUser = localStorage.getItem('userData');
    if (loggedInUser) {
      const userData = JSON.parse(loggedInUser);
      this.loggedinUserId = userData.userId;
    }
    console.log("Loggedin user ID: ", this.loggedinUserId)

    this.route.paramMap.subscribe(params => {
      let encodedPrimaryData = params.get('data') || "";
      this.primaryData = JSON.parse(decode(encodedPrimaryData));
      if (this.primaryData.answersheetId) {
        this.getAnswersheetMark();
        this.getQuestionPaperAnswerKey(this.primaryData.answersheetId);
      }
    });

  }

  getAnswersheetMark() {
    this.evaluationService.getAnswersheetMark(this.primaryData.answersheetId).subscribe(
      (data: any) => {
        console.log("saved mark data: ", data)
        this.answersheetMarkData = data;
      },
      (error) => {
        console.error('Error getting mark data:', error);
        this.toastr.error('Failed to get mark data.');
      }
    )
  }

  getQuestionPaperAnswerKey(answerSheetId: number) {
    console.log('loading questions............');
    this.evaluationService.getQuestionAndAnswer(answerSheetId).subscribe(
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
          this.getGroupList(data);

          this.obtainedMarks = this.qaList.reduce((sum, item) => sum + (item.obtainedMark || 0), 0);
          this.reduceChoiceQuestionMarks();
          
        }
      },
      (error) => {
        console.error('Error fetching question and answer:', error);
        this.toastr.error('Failed to load question and answer.');
      }
    );
  }

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

  getPartList(data: any[]) {
    console.log("getPartList...............");
    const partMap = new Map();
    data.forEach(({ questionPartName }) => {
      partMap.set(questionPartName, (partMap.get(questionPartName) || 0) + 1);
    });
    this.partList = Array.from(partMap, ([part, count]) => ({ part, count }));
    console.log("partList:", this.partList);
  }

  getGroupList(data: any[]) {
    console.log("getGroupList...............");
    const groupMap = new Map();
    data.forEach(({ questionGroupName }) => {
      groupMap.set(questionGroupName, (groupMap.get(questionGroupName) || 0) + 1);
    });
    this.groupList = Array.from(groupMap, ([group, count]) => ({ group, count }))
      .filter(({ count }) => count > 1);
    console.log("groupList:", this.groupList);
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

    if(matchedItem) {
      this.activeQuestion = matchedItem.questionDescription;
      this.activeQuestionImg = matchedItem.questionImage;
      this.activeAnswerKey = matchedItem.answerDescription;
      this.activeQuestionMark = matchedItem.mark;
    }
    
  }

  validateMark(event: any, qno: number, qsno: number) {
    if (event.target.value) {
      
      if (event.target.value.match(/[^0-9]/g)) {
        this.toastr.error('Please add only numbers.');
        event.target.value = event.target.value.replace(/[^\d]/g, '');
        return;
      }
      else if ((parseFloat(event.target.value) > parseInt(event.srcElement.max))) {
        let msg = `Maximum marks allowed is ${event.srcElement.max}`
        this.toastr.error(msg);
        event.target.value = '';
        return;
      }
      else {
        this.saveMark(qno, qsno, parseInt(event.srcElement.max), parseFloat(event.target.value));
      }

      this.obtainedMarks = 0;
      let txtList = document.querySelectorAll(".question .form-control") as NodeListOf<HTMLInputElement>;
      txtList.forEach((item: HTMLInputElement) => {
        if (item.value) {
          this.obtainedMarks += parseFloat(item.value); // sum all text box marks
        }
      });

    }
  }

  saveMark(qno: number, qsno: number, maxMark: number, obtainedMark: number) {
    this.answersheetMark = {
      "createdById": this.loggedinUserId,
      "modifiedById": this.loggedinUserId,
      "answersheetId": this.primaryData.answersheetId,
      "questionNumber": qno,
      "questionNumberSubNum": qsno,
      "maximumMark": maxMark,
      "obtainedMark": obtainedMark
    }

    console.log("answersheetMark: ", this.answersheetMark)

    this.evaluationService.saveAnswersheetMark(this.answersheetMark).subscribe(
      (data: any) => {
        console.log("save data: ", data)
        if (data.message.toLowerCase() == 'success') {          
          this.toastr.success("Mark auto saved.");
        }
      },
      (error) => {
        console.error('Error saving mark:', error);
        this.toastr.error('Failed to save mark.');
      }
    )
  }

  submitEvaluation() {
    if (this.obtainedMarks == 0) {
      this.toastr.warning('Obained Marks is 0. Please evaluate.');
    }
    else if (this.obtainedMarks > this.totalMarks) {
      this.toastr.error('Obtained Marks are more than Total marks. Please verify.');
    }
    else {
      this.toastr.success("Evaluation submitted successfully");
    }
  }

  filter(part: string) {
    let list = this.qaList.filter(e => e.part === part);
    return list;
  }

  onError(error: any) {
    console.error('PDF error: ', error);
  }

}
