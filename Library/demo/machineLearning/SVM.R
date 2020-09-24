imports "SVM" from "MLkit";

let svm = svm.problem(["X", "Y"])
:> append.trainingSet(
	tag = 10,
	data = data.frame(X = runif(100, -120, -100), Y = runif(100,1,2))
)
:> append.trainingSet(
	tag = 2,
	data = data.frame(X = runif(100, 1, 10), Y = runif(100, 0, 20))
)
:> append.trainingSet(
	tag = 3,
	data = data.frame(X = runif(100, 300, 500), Y = runif(100, 300, 310))
)
:> append.trainingSet(
	tag = "332A",
	data = data.frame(X = runif(100, 300, 500), Y = runif(100, 1300, 1310))
)
:> trainSVMModel
;

# 2 10 2 3 332A 2
let validates = data.frame(X = [2,-103,3,311,500, 50], Y = [1,2,1.3,302,1800, 50]);

rownames(validates) = ["a","b","c","d","e", "f"];

str(svm :> svm_classify(validates));

const json_saved = `${!script$dir}/SVM.json`;

print(`the svm model in json format will be saved at location: ${json_saved}`);

svm 
:> svm_json
:> writeLines(con = json_saved)
;

print("validate result from the json model loaded result:");

json_saved
:> readText
:> parse.SVM_json
:> svm_classify(validates)
:> str
;