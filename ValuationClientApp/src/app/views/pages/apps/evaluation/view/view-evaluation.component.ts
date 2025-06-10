import { NgClass, NgFor } from '@angular/common';
import { NgbModalRef, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { OnInit, Component, ViewChild, ChangeDetectorRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PdfViewerModule, PDFDocumentProxy } from 'ng2-pdf-viewer';
import { ToastrService } from 'ngx-toastr';
import { EvaluationService } from '../../../services/evaluation.service';
import { encode, decode } from 'js-base64';
import { ActivatedRoute } from '@angular/router'
import { AnswersheetMark } from '../../../models/answersheetMark.model';
import { Router } from '@angular/router';
import { SpinnerService } from '../../../services/spinner.service';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  selector: 'app-view-evaluation',
  imports: [NgClass, NgFor, CommonModule, PdfViewerModule],
  templateUrl: './view-evaluation.component.html',
  styleUrl: './view-evaluation.component.scss'
})

export class ViewEvaluationComponent implements OnInit, AfterViewChecked {
  modalRef: NgbModalRef;
  loggedinUserId: number = 0;
  answersheetId: number = 0;
  primaryData: any;
  answersheetMarkData: any[] = [];
  qaList: any[] = [];
  partList: any[] = [];
  groupList: any[] = [];
  partAMark: number = 0;
  partBMark: number = 0;
  partCMark: number = 0;
  obtainedMarks: number = 0;
  activeQuestionNo: string = '-';
  activeQuestion: SafeHtml = '---------- Please select the question above to load here ----------';
  activeQuestionImg: string = '';
  activeAnswerKey: SafeHtml = '---------- Answerkey loads here ----------';
  activeAnswerImg: string = '';  
  activeQuestionMark: string = '-';
  answersheet: string = '---------- Answersheet loads here ----------';
  answersheetMark: AnswersheetMark;
  sasToken: string = "sp=r&st=2025-04-23T08:48:04Z&se=2025-04-23T16:48:04Z&sv=2024-11-04&sr=c&sig=M%2F90Dwk7LJjwQPE%2FTsYmGnIKl1gpgk%2Fvtbp63LNU5qs%3D"

  noMarksList: string = '';
  currentPage: number = 3; // Hide Page 1 ,2 , - it has student details
  totalPages: number = 0;
  enableSubmit: boolean = false;

  @ViewChild('confirmModule') confirmModule: any;

  constructor(
    private modalService: NgbModal,
    private route: ActivatedRoute,
    private toastr: ToastrService,
    private evaluationService: EvaluationService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private spinnerService: SpinnerService,
    private sanitizer: DomSanitizer
  ) { }

  ngOnInit() {
    console.log("ngOnInit.................");

    this.spinnerService.toggleSpinnerState(true);
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

  // called after dom is loaded to calclate the marks
  ngAfterViewChecked(): void {
    //this.cdr.detectChanges();
    setTimeout(() => {
      this.calculateSubTotalMarks();
    }, 3000);    
  }

  async getPrimaryData() {
    //console.log('loading primary data............');
    await this.evaluationService.getAnswersheetDetailsById(this.answersheetId).subscribe(
      (data: any) => {
        if (data[0]) {
          console.log("primary data: ", data[0])
          this.primaryData = data[0];
          this.answersheet = `${this.primaryData.uploadedBlobStorageUrl}`;
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
              "answerImage": decode(item.answerImage),
              "mark": this.getMark(item.questionMark),
              "disableInput": this.disableInput(item.questionMark),
              "obtainedMark": this.getSavedMarks(item.questionNumber, item.questionNumberSubNum)
            });
          });

          console.log("qaList: ", this.qaList);

          this.getPartList(data);
          this.getGroupList(this.qaList);

          //this.obtainedMarks = this.qaList.reduce((sum, item) => sum + (item.obtainedMark || 0), 0);
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

  getGroupList(data: any[]) {
    //console.log("getGroupList...............");    

    var markArray: any[] = []

    data.forEach(item => {
      if (item.questionNumber >= 10) {
        markArray.push({
          "group": item.questionNumber,
          "count": item.questionNumber == 10 ? 10 : 1,
          "mark": item.obtainedMark
        })
      }
    })

    //console.log("markArray: ", JSON.stringify(markArray))

    const groupMap = new Map<number, { group: number, count: number, mark: number }>();

    markArray.forEach(item => {
      if (!groupMap.has(item.group)) {
        groupMap.set(item.group, { group: item.group, count: 0, mark: 0 });
      }
      const current = groupMap.get(item.group)!;
      current.count += item.count;
      current.mark += item.mark;
    });

    this.groupList = Array.from(groupMap.values());

    console.log("groupList:", (this.groupList));
  }

  getSavedMarks(questionNumber: number, questionSubNum: number) {
    let questionRow = this.answersheetMarkData.filter(x => x.questionNumber == questionNumber && x.questionNumberSubNum == questionSubNum)[0];
    if (questionRow) {
      //console.log(questionRow.obtainedMark);
      return questionRow.obtainedMark;
    }
    else {
      return 0;
    }
  }

  calculateSubTotalMarks() {
    this.partAMark = 0;
    this.partBMark = 0;
    this.partCMark = 0;
    var partBarray: any[] = [];
    var partCarray: any[] = [];

    let partAList = document.querySelectorAll("[part]") as NodeListOf<HTMLInputElement>;
    partAList.forEach((item: HTMLInputElement) => {
      if (item.getAttribute('part') == 'A') {
        if (item.value) {
          this.partAMark += parseFloat(item.value);
        }
      }
      else if (item.getAttribute('part') == 'B') {
        if (item.value) {
          this.getTotalMarkFromGroup(item, partBarray);
        }
      }
      else if (item.getAttribute('part') == 'C') {
        if (item.value) {
          this.getTotalMarkFromGroup(item, partCarray);
        }
      }
    });
  }

  getTotalMarkFromGroup(item: HTMLInputElement, partArray: any[]) {
    partArray.push({
      "qno": item.getAttribute('qno'),
      "group": item.getAttribute('group'),
      "mark": item.value,
    })

    // Step 1: Sum marks by (qno + group)
    const summed: any[] = Object.values(
      partArray.reduce((acc, item) => {
        const key = `${item.qno}_${item.group}`;
        const mark = Number(item.mark);

        if (!acc[key]) {
          acc[key] = { qno: item.qno, group: item.group, totalMark: mark };
        } else {
          acc[key].totalMark += mark;
        }

        return acc;
      }, {} as { [key: string]: { qno: string, group: string, totalMark: number } })
    );

    //console.log("summed:", summed);

    // 
    let subTotalList = document.querySelectorAll("span.subtotal");
    subTotalList.forEach((item) => {
      summed.forEach((itm: { qno: string | null; totalMark: string; }) => {
        if (item.getAttribute('qno') == itm.qno) {
          item.innerHTML = itm.totalMark;
        }
      });
    });

    //console.log("subTotalList:", subTotalList);

    // Step 2: From each group, pick the one with highest totalMark
    const finalResult = Object.values(
      summed.reduce((acc, item) => {
        const group = item.group;

        if (!acc[group] || item.totalMark > acc[group].totalMark) {
          acc[group] = item;
        }

        return acc;
      }, {} as { [group: string]: { qno: string, group: string, totalMark: number } })
    );

    //console.log("finalResult:", finalResult);

    // Step 3: Sum of all totalMarks
    const sumTotalMarks: any = finalResult.reduce((acc, item: any) => acc + item.totalMark, 0);

    this.partBMark = parseFloat(sumTotalMarks);
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

    //this.totalMarks = this.totalMarks + mark;

    return mark;
  }

  disableInput(questionMark: string) {
    let mark = 0;
    const dom = new DOMParser().parseFromString(decode(questionMark), 'text/html');
    let spans = dom.querySelectorAll('span');

    spans.forEach((span) => {
      let innerHTML = span.innerHTML.replace(/&nbsp;/g, '').trim();
      //console.log(innerHTML);
      if ((innerHTML.length > 0)) {
        mark = parseInt(innerHTML);
      }
    });

    //this.totalMarks = this.totalMarks + mark;

    return (String(mark) == 'NaN') ? true : false;
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
      if ((!matchedItem.questionImage) && (!matchedItem.answerImage)) {
        this.loadQuestionAnswerImages(qusetionNo, matchedItem.questionNumber, matchedItem.questionNumberSubNum);
      }
      else {        
        this.activeQuestionImg = decode(matchedItem.questionImage);
        this.activeAnswerImg = decode(matchedItem.answerImage);
      }
      this.activeQuestionNo = matchedItem.questionNumberDisplay;
      this.activeQuestion = this.sanitizer.bypassSecurityTrustHtml(matchedItem.questionDescription);
      this.activeAnswerKey = this.sanitizer.bypassSecurityTrustHtml(matchedItem.answerDescription);
      this.activeQuestionMark = matchedItem.mark;
    }

  }

  loadQuestionAnswerImages(qusetionDisplayNo: string, questionNumber: number, questionNumberSubNum: number) {
    this.spinnerService.toggleSpinnerState(true);
    this.evaluationService.getQuestionAndAnswerImages(this.answersheetId, questionNumber, questionNumberSubNum).subscribe(
      (data: any[]) => {
        if (data.length > 0) {
          this.activeQuestionImg = decode(data[0].questionImage);
          this.activeAnswerImg = decode(data[0].answerImage);

          this.qaList = this.qaList.map(e =>
            e.questionNumberDisplay === qusetionDisplayNo ? { ...e, questionImage: data[0].questionImage, answerImage: data[0].answerImage } : e
          );
        }
        else {
          console.log('No question and answer image data');
        }
        this.spinnerService.toggleSpinnerState(false);
      },
      (error) => {
        console.error('Error fetching question and answer image:', error);
        this.toastr.error('Failed to load question and answer image.');
      }
    );
  }

  validateMark(event: any, item: any) {
    if (event.target.value) {
      if (event.target.value.match(/[^0-9.]/g)) {
        this.toastr.error('Please add only numbers.');
        event.target.value = ''; //event.target.value.replace(/[^\d]/g, '');
        this.saveMark(item, 0);
        //return;
      }
      else if ((parseFloat(event.target.value) > parseInt(event.srcElement.max))) {
        let msg = `Maximum marks allowed is ${event.srcElement.max}`
        this.toastr.error(msg);
        event.target.value = '';
        this.saveMark(item, 0);
        //return;
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
    this.checkMissingMarks();
    this.modalRef = this.modalService.open(this.confirmModule, { size: 'lg', backdrop: 'static' });
  }

  completeEvaluation() {
    this.modalRef.close();
    if (this.obtainedMarks == 0) {
      this.toastr.warning('Obained Marks is 0. Please evaluate.');
    }
    else {
      this.evaluationService.completeEvaluation(this.primaryData.answersheetId, this.loggedinUserId).subscribe(
        (data: any) => {
          console.log("respo", data)
          if (data.message.toLowerCase() == 'success') {
            this.toastr.success("Evaluation completed successfully");
            this.backToList();
          }
          else {
            console.error('Failed to complete evaluation');
            this.toastr.error('Failed to complete evaluation.');
          }
        },
        (error) => {
          console.error('Error while saving evaluation:', error);
          this.toastr.error('Failed to complete evaluation.');
        }
      )
    }
  }

  checkMissingMarks() {

    this.noMarksList = '';
    let qList = document.querySelectorAll("[qgno]") as NodeListOf<HTMLInputElement>;
    qList.forEach((item: HTMLInputElement) => {
      if (!item.value && item.getAttribute('max') != 'NaN') {
        this.noMarksList += item.getAttribute('qgno') + ', '
      }
    });

    if (this.noMarksList) {
      this.noMarksList = this.noMarksList.trim().slice(0, -1);
    }

  }

  onPdfLoad(pdf: PDFDocumentProxy) {
    this.totalPages = pdf.numPages;
    this.spinnerService.toggleSpinnerState(false);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) this.currentPage++;
    if (this.currentPage == this.totalPages) this.enableSubmit = true;
  }

  prevPage() {
    if (this.currentPage > 3) this.currentPage--;
  }

  backToList() {
    this.router.navigate(['/apps/answersheet']);
  }

  onError(error: any) {
    console.error('PDF error: ', error);
  }

}
