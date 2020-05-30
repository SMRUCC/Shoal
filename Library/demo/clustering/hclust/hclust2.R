imports "stats.clustering" from "R.math";
imports "charts" from "R.plot";

setwd(!script$dir);

let sampleInfo = read.csv("E:\GCModeller\src\R-sharp\Library\demo\sampleInfo.csv");
let labels = sampleInfo[, "ID"];
let class = `#${as.character(sampleInfo[, "color"])}`;

class = lapply(1:length(labels), i -> class[i], names = i -> labels[i]);

str(class);

let d = read.csv("E:\GCModeller\src\R-sharp\Library\demo\metabolome.txt", row_names = 1, tsv = TRUE)
:> t
:> dist
;

print(d);

d
:> hclust
:> plot(class = class, padding = "padding: 200px 200px 200px 200px;")
:> save.graphics("./hclust2.png")