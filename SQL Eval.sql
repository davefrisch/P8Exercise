USE Pulse8TestDB

SELECT	MemberID, FirstName, LastName, MostSevereDiagnosisId, MostSevereDiagnosisDescription, CategoryId, CategoryDescription, CategoryScore, IsMostSevereCategory
FROM(
	SELECT		dM.MemberID, dM.FirstName, dM.LastName
				,CASE WHEN RANK()OVER(PARTITION BY dM.MemberId, dDC.DiagnosisCategoryID ORDER BY dD.DiagnosisId) = 1 THEN dD.DiagnosisID END AS MostSevereDiagnosisId/*Identity the lowest DiagnosisId*/
				,CASE WHEN RANK()OVER(PARTITION BY dM.MemberId, dDC.DiagnosisCategoryID ORDER BY dD.DiagnosisId) = 1 THEN dD.DiagnosisDescription END AS MostSevereDiagnosisDescription/*Identify diagnosis description of lowest DiagnosisId*/
				,dDC.DiagnosisCategoryID AS CategoryId
				,dDC.CategoryDescription
				,dDC.CategoryScore
				,CASE WHEN RANK()OVER(PARTITION BY dM.MemberId ORDER BY lDC.DiagnosisCategoryId) = 1 THEN 1 WHEN dDC.DiagnosisCategoryID IS NULL THEN 1 ELSE 0 END AS IsMostSevereCategory /*Identify most severe category and set to 0 if no DiagnosisCategoryId exists*/
				,ROW_NUMBER()OVER(PARTITION BY dM.MemberId, dDC.DiagnosisCategoryID ORDER BY dD.DiagnosisId) AS SequenceId /*Identify duplicating categories and filter them out in the higher query*/
	FROM		dbo.Member dM 
	LEFT JOIN	dbo.MemberDiagnosis lMD 
				ON lMD.MemberID = dM.MemberID
	LEFT JOIN	dbo.Diagnosis dD 
				ON dD.DiagnosisID = lMD.DiagnosisID
	LEFT JOIN	dbo.DiagnosisCategoryMap lDC 
				ON lDC.DiagnosisID = lMD.DiagnosisID
	LEFT JOIN	dbo.DiagnosisCategory dDC 
				ON dDC.DiagnosisCategoryID = lDC.DiagnosisCategoryID
)X
WHERE		SequenceId = 1
ORDER BY	MemberID, ISNULL(MostSevereDiagnosisId,99), ISNULL(CategoryId,99)

/*
Used RANK for the "MostSevere" columns originally because I was concerned there may have been duplicating diagnoses, and I wanted to see them before I decide how to handle that.
Used ROW_NUMBER to remove duplicating categories because that's just what my mind defaults to.
Left joins because of Jack Smith and others who don't have any diagnosis.
Threw it in a subquery with an outside WHERE clause, but considered CTE, Temp table, or table variable as well.
*/


/*

SELECT	*
FROM	dbo.Diagnosis --<- Looks like a dimension table. 

SELECT	*
FROM	dbo.DiagnosisCategory --<- Looks like a dimension table expanding on the above

SELECT	*
FROM	dbo.DiagnosisCategoryMap --<-Bridge table (factless fact) between DiagnosisCategory and Diagnosis

SELECT	*
FROM	dbo.Member --<- Dimension table for individuals/patients/the insured otherwise known as members

SELECT	*
FROM	dbo.MemberDiagnosis --<- Looks like a fact table related to members and their associated diagnosis

*/


/*
select	*
FROM		dbo.Member dM 
	LEFT JOIN	dbo.MemberDiagnosis lMD 
				ON lMD.MemberID = dM.MemberID
	LEFT JOIN	dbo.Diagnosis dD 
				ON dD.DiagnosisID = lMD.DiagnosisID
	LEFT JOIN	dbo.DiagnosisCategoryMap lDC 
				ON lDC.DiagnosisID = lMD.DiagnosisID
	LEFT JOIN	dbo.DiagnosisCategory dDC 
				ON dDC.DiagnosisCategoryID = lDC.DiagnosisCategoryID
*/