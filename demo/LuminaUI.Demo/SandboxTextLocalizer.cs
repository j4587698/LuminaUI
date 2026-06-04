using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using LuminaUI.Controls;
using LuminaUI.Localization;

namespace LuminaUI.Demo;

internal static class SandboxTextLocalizer
{
	private static readonly Lazy<Dictionary<string, string>> Lookup = new Lazy<Dictionary<string, string>>(CreateLookup);

	private static readonly string[] TextKeys = new string[1364]
	{
		"Sandbox.Text.0001", "Sandbox.Text.0002", "Sandbox.Text.0003", "Sandbox.Text.0004", "Sandbox.Text.0005", "Sandbox.Text.0006", "Sandbox.Text.0007", "Sandbox.Text.0008", "Sandbox.Text.0009", "Sandbox.Text.0010",
		"Sandbox.Text.0011", "Sandbox.Text.0012", "Sandbox.Text.0013", "Sandbox.Text.0014", "Sandbox.Text.0015", "Sandbox.Text.0016", "Sandbox.Text.0017", "Sandbox.Text.0018", "Sandbox.Text.0019", "Sandbox.Text.0020",
		"Sandbox.Text.0021", "Sandbox.Text.0022", "Sandbox.Text.0023", "Sandbox.Text.0024", "Sandbox.Text.0025", "Sandbox.Text.0026", "Sandbox.Text.0027", "Sandbox.Text.0028", "Sandbox.Text.0029", "Sandbox.Text.0030",
		"Sandbox.Text.0031", "Sandbox.Text.0032", "Sandbox.Text.0033", "Sandbox.Text.0034", "Sandbox.Text.0035", "Sandbox.Text.0036", "Sandbox.Text.0037", "Sandbox.Text.0038", "Sandbox.Text.0039", "Sandbox.Text.0040",
		"Sandbox.Text.0041", "Sandbox.Text.0042", "Sandbox.Text.0043", "Sandbox.Text.0044", "Sandbox.Text.0045", "Sandbox.Text.0046", "Sandbox.Text.0047", "Sandbox.Text.0048", "Sandbox.Text.0049", "Sandbox.Text.0050",
		"Sandbox.Text.0051", "Sandbox.Text.0052", "Sandbox.Text.0053", "Sandbox.Text.0054", "Sandbox.Text.0055", "Sandbox.Text.0056", "Sandbox.Text.0057", "Sandbox.Text.0058", "Sandbox.Text.0059", "Sandbox.Text.0060",
		"Sandbox.Text.0061", "Sandbox.Text.0062", "Sandbox.Text.0063", "Sandbox.Text.0064", "Sandbox.Text.0065", "Sandbox.Text.0066", "Sandbox.Text.0067", "Sandbox.Text.0068", "Sandbox.Text.0069", "Sandbox.Text.0070",
		"Sandbox.Text.0071", "Sandbox.Text.0072", "Sandbox.Text.0073", "Sandbox.Text.0074", "Sandbox.Text.0075", "Sandbox.Text.0076", "Sandbox.Text.0077", "Sandbox.Text.0078", "Sandbox.Text.0079", "Sandbox.Text.0080",
		"Sandbox.Text.0081", "Sandbox.Text.0082", "Sandbox.Text.0083", "Sandbox.Text.0084", "Sandbox.Text.0085", "Sandbox.Text.0086", "Sandbox.Text.0087", "Sandbox.Text.0088", "Sandbox.Text.0089", "Sandbox.Text.0090",
		"Sandbox.Text.0091", "Sandbox.Text.0092", "Sandbox.Text.0093", "Sandbox.Text.0094", "Sandbox.Text.0095", "Sandbox.Text.0096", "Sandbox.Text.0097", "Sandbox.Text.0098", "Sandbox.Text.0099", "Sandbox.Text.0100",
		"Sandbox.Text.0101", "Sandbox.Text.0102", "Sandbox.Text.0103", "Sandbox.Text.0104", "Sandbox.Text.0105", "Sandbox.Text.0106", "Sandbox.Text.0107", "Sandbox.Text.0108", "Sandbox.Text.0109", "Sandbox.Text.0110",
		"Sandbox.Text.0111", "Sandbox.Text.0112", "Sandbox.Text.0113", "Sandbox.Text.0114", "Sandbox.Text.0115", "Sandbox.Text.0116", "Sandbox.Text.0117", "Sandbox.Text.0118", "Sandbox.Text.0119", "Sandbox.Text.0120",
		"Sandbox.Text.0121", "Sandbox.Text.0122", "Sandbox.Text.0123", "Sandbox.Text.0124", "Sandbox.Text.0125", "Sandbox.Text.0126", "Sandbox.Text.0127", "Sandbox.Text.0128", "Sandbox.Text.0129", "Sandbox.Text.0130",
		"Sandbox.Text.0131", "Sandbox.Text.0132", "Sandbox.Text.0133", "Sandbox.Text.0134", "Sandbox.Text.0135", "Sandbox.Text.0136", "Sandbox.Text.0137", "Sandbox.Text.0138", "Sandbox.Text.0139", "Sandbox.Text.0140",
		"Sandbox.Text.0141", "Sandbox.Text.0142", "Sandbox.Text.0143", "Sandbox.Text.0144", "Sandbox.Text.0145", "Sandbox.Text.0146", "Sandbox.Text.0147", "Sandbox.Text.0148", "Sandbox.Text.0149", "Sandbox.Text.0150",
		"Sandbox.Text.0151", "Sandbox.Text.0152", "Sandbox.Text.0153", "Sandbox.Text.0154", "Sandbox.Text.0155", "Sandbox.Text.0156", "Sandbox.Text.0157", "Sandbox.Text.0158", "Sandbox.Text.0159", "Sandbox.Text.0160",
		"Sandbox.Text.0161", "Sandbox.Text.0162", "Sandbox.Text.0163", "Sandbox.Text.0164", "Sandbox.Text.0165", "Sandbox.Text.0166", "Sandbox.Text.0167", "Sandbox.Text.0168", "Sandbox.Text.0169", "Sandbox.Text.0170",
		"Sandbox.Text.0171", "Sandbox.Text.0172", "Sandbox.Text.0173", "Sandbox.Text.0174", "Sandbox.Text.0175", "Sandbox.Text.0176", "Sandbox.Text.0177", "Sandbox.Text.0178", "Sandbox.Text.0179", "Sandbox.Text.0180",
		"Sandbox.Text.0181", "Sandbox.Text.0182", "Sandbox.Text.0183", "Sandbox.Text.0184", "Sandbox.Text.0185", "Sandbox.Text.0186", "Sandbox.Text.0187", "Sandbox.Text.0188", "Sandbox.Text.0189", "Sandbox.Text.0190",
		"Sandbox.Text.0191", "Sandbox.Text.0192", "Sandbox.Text.0193", "Sandbox.Text.0194", "Sandbox.Text.0195", "Sandbox.Text.0196", "Sandbox.Text.0197", "Sandbox.Text.0198", "Sandbox.Text.0199", "Sandbox.Text.0200",
		"Sandbox.Text.0201", "Sandbox.Text.0202", "Sandbox.Text.0203", "Sandbox.Text.0204", "Sandbox.Text.0205", "Sandbox.Text.0206", "Sandbox.Text.0207", "Sandbox.Text.0208", "Sandbox.Text.0209", "Sandbox.Text.0210",
		"Sandbox.Text.0211", "Sandbox.Text.0212", "Sandbox.Text.0213", "Sandbox.Text.0214", "Sandbox.Text.0215", "Sandbox.Text.0216", "Sandbox.Text.0217", "Sandbox.Text.0218", "Sandbox.Text.0219", "Sandbox.Text.0220",
		"Sandbox.Text.0221", "Sandbox.Text.0222", "Sandbox.Text.0223", "Sandbox.Text.0224", "Sandbox.Text.0225", "Sandbox.Text.0226", "Sandbox.Text.0227", "Sandbox.Text.0228", "Sandbox.Text.0229", "Sandbox.Text.0230",
		"Sandbox.Text.0231", "Sandbox.Text.0232", "Sandbox.Text.0233", "Sandbox.Text.0234", "Sandbox.Text.0235", "Sandbox.Text.0236", "Sandbox.Text.0237", "Sandbox.Text.0238", "Sandbox.Text.0239", "Sandbox.Text.0240",
		"Sandbox.Text.0241", "Sandbox.Text.0242", "Sandbox.Text.0243", "Sandbox.Text.0244", "Sandbox.Text.0245", "Sandbox.Text.0246", "Sandbox.Text.0247", "Sandbox.Text.0248", "Sandbox.Text.0249", "Sandbox.Text.0250",
		"Sandbox.Text.0251", "Sandbox.Text.0252", "Sandbox.Text.0253", "Sandbox.Text.0254", "Sandbox.Text.0255", "Sandbox.Text.0256", "Sandbox.Text.0257", "Sandbox.Text.0258", "Sandbox.Text.0259", "Sandbox.Text.0260",
		"Sandbox.Text.0261", "Sandbox.Text.0262", "Sandbox.Text.0263", "Sandbox.Text.0264", "Sandbox.Text.0265", "Sandbox.Text.0266", "Sandbox.Text.0267", "Sandbox.Text.0268", "Sandbox.Text.0269", "Sandbox.Text.0270",
		"Sandbox.Text.0271", "Sandbox.Text.0272", "Sandbox.Text.0273", "Sandbox.Text.0274", "Sandbox.Text.0275", "Sandbox.Text.0276", "Sandbox.Text.0277", "Sandbox.Text.0278", "Sandbox.Text.0279", "Sandbox.Text.0280",
		"Sandbox.Text.0281", "Sandbox.Text.0282", "Sandbox.Text.0283", "Sandbox.Text.0284", "Sandbox.Text.0285", "Sandbox.Text.0286", "Sandbox.Text.0287", "Sandbox.Text.0288", "Sandbox.Text.0289", "Sandbox.Text.0290",
		"Sandbox.Text.0291", "Sandbox.Text.0292", "Sandbox.Text.0293", "Sandbox.Text.0294", "Sandbox.Text.0295", "Sandbox.Text.0296", "Sandbox.Text.0297", "Sandbox.Text.0298", "Sandbox.Text.0299", "Sandbox.Text.0300",
		"Sandbox.Text.0301", "Sandbox.Text.0302", "Sandbox.Text.0303", "Sandbox.Text.0304", "Sandbox.Text.0305", "Sandbox.Text.0306", "Sandbox.Text.0307", "Sandbox.Text.0308", "Sandbox.Text.0309", "Sandbox.Text.0310",
		"Sandbox.Text.0311", "Sandbox.Text.0312", "Sandbox.Text.0313", "Sandbox.Text.0314", "Sandbox.Text.0315", "Sandbox.Text.0316", "Sandbox.Text.0317", "Sandbox.Text.0318", "Sandbox.Text.0319", "Sandbox.Text.0320",
		"Sandbox.Text.0321", "Sandbox.Text.0322", "Sandbox.Text.0323", "Sandbox.Text.0324", "Sandbox.Text.0325", "Sandbox.Text.0326", "Sandbox.Text.0327", "Sandbox.Text.0328", "Sandbox.Text.0329", "Sandbox.Text.0330",
		"Sandbox.Text.0331", "Sandbox.Text.0332", "Sandbox.Text.0333", "Sandbox.Text.0334", "Sandbox.Text.0335", "Sandbox.Text.0336", "Sandbox.Text.0337", "Sandbox.Text.0338", "Sandbox.Text.0339", "Sandbox.Text.0340",
		"Sandbox.Text.0341", "Sandbox.Text.0342", "Sandbox.Text.0343", "Sandbox.Text.0344", "Sandbox.Text.0345", "Sandbox.Text.0346", "Sandbox.Text.0347", "Sandbox.Text.0348", "Sandbox.Text.0349", "Sandbox.Text.0350",
		"Sandbox.Text.0351", "Sandbox.Text.0352", "Sandbox.Text.0353", "Sandbox.Text.0354", "Sandbox.Text.0355", "Sandbox.Text.0356", "Sandbox.Text.0357", "Sandbox.Text.0358", "Sandbox.Text.0359", "Sandbox.Text.0360",
		"Sandbox.Text.0361", "Sandbox.Text.0362", "Sandbox.Text.0363", "Sandbox.Text.0364", "Sandbox.Text.0365", "Sandbox.Text.0366", "Sandbox.Text.0367", "Sandbox.Text.0368", "Sandbox.Text.0369", "Sandbox.Text.0370",
		"Sandbox.Text.0371", "Sandbox.Text.0372", "Sandbox.Text.0373", "Sandbox.Text.0374", "Sandbox.Text.0375", "Sandbox.Text.0376", "Sandbox.Text.0377", "Sandbox.Text.0378", "Sandbox.Text.0379", "Sandbox.Text.0380",
		"Sandbox.Text.0381", "Sandbox.Text.0382", "Sandbox.Text.0383", "Sandbox.Text.0384", "Sandbox.Text.0385", "Sandbox.Text.0386", "Sandbox.Text.0387", "Sandbox.Text.0388", "Sandbox.Text.0389", "Sandbox.Text.0390",
		"Sandbox.Text.0391", "Sandbox.Text.0392", "Sandbox.Text.0393", "Sandbox.Text.0394", "Sandbox.Text.0395", "Sandbox.Text.0396", "Sandbox.Text.0397", "Sandbox.Text.0398", "Sandbox.Text.0399", "Sandbox.Text.0400",
		"Sandbox.Text.0401", "Sandbox.Text.0402", "Sandbox.Text.0403", "Sandbox.Text.0404", "Sandbox.Text.0405", "Sandbox.Text.0406", "Sandbox.Text.0407", "Sandbox.Text.0408", "Sandbox.Text.0409", "Sandbox.Text.0410",
		"Sandbox.Text.0411", "Sandbox.Text.0412", "Sandbox.Text.0413", "Sandbox.Text.0414", "Sandbox.Text.0415", "Sandbox.Text.0416", "Sandbox.Text.0417", "Sandbox.Text.0418", "Sandbox.Text.0419", "Sandbox.Text.0420",
		"Sandbox.Text.0421", "Sandbox.Text.0422", "Sandbox.Text.0423", "Sandbox.Text.0424", "Sandbox.Text.0425", "Sandbox.Text.0426", "Sandbox.Text.0427", "Sandbox.Text.0428", "Sandbox.Text.0429", "Sandbox.Text.0430",
		"Sandbox.Text.0431", "Sandbox.Text.0432", "Sandbox.Text.0433", "Sandbox.Text.0434", "Sandbox.Text.0435", "Sandbox.Text.0436", "Sandbox.Text.0437", "Sandbox.Text.0438", "Sandbox.Text.0439", "Sandbox.Text.0440",
		"Sandbox.Text.0441", "Sandbox.Text.0442", "Sandbox.Text.0443", "Sandbox.Text.0444", "Sandbox.Text.0445", "Sandbox.Text.0446", "Sandbox.Text.0447", "Sandbox.Text.0448", "Sandbox.Text.0449", "Sandbox.Text.0450",
		"Sandbox.Text.0451", "Sandbox.Text.0452", "Sandbox.Text.0453", "Sandbox.Text.0454", "Sandbox.Text.0455", "Sandbox.Text.0456", "Sandbox.Text.0457", "Sandbox.Text.0458", "Sandbox.Text.0459", "Sandbox.Text.0460",
		"Sandbox.Text.0461", "Sandbox.Text.0462", "Sandbox.Text.0463", "Sandbox.Text.0464", "Sandbox.Text.0465", "Sandbox.Text.0466", "Sandbox.Text.0467", "Sandbox.Text.0468", "Sandbox.Text.0469", "Sandbox.Text.0470",
		"Sandbox.Text.0471", "Sandbox.Text.0472", "Sandbox.Text.0473", "Sandbox.Text.0474", "Sandbox.Text.0475", "Sandbox.Text.0476", "Sandbox.Text.0477", "Sandbox.Text.0478", "Sandbox.Text.0479", "Sandbox.Text.0480",
		"Sandbox.Text.0481", "Sandbox.Text.0482", "Sandbox.Text.0483", "Sandbox.Text.0484", "Sandbox.Text.0485", "Sandbox.Text.0486", "Sandbox.Text.0487", "Sandbox.Text.0488", "Sandbox.Text.0489", "Sandbox.Text.0490",
		"Sandbox.Text.0491", "Sandbox.Text.0492", "Sandbox.Text.0493", "Sandbox.Text.0494", "Sandbox.Text.0495", "Sandbox.Text.0496", "Sandbox.Text.0497", "Sandbox.Text.0498", "Sandbox.Text.0499", "Sandbox.Text.0500",
		"Sandbox.Text.0501", "Sandbox.Text.0502", "Sandbox.Text.0503", "Sandbox.Text.0504", "Sandbox.Text.0505", "Sandbox.Text.0506", "Sandbox.Text.0507", "Sandbox.Text.0508", "Sandbox.Text.0509", "Sandbox.Text.0510",
		"Sandbox.Text.0511", "Sandbox.Text.0512", "Sandbox.Text.0513", "Sandbox.Text.0514", "Sandbox.Text.0515", "Sandbox.Text.0516", "Sandbox.Text.0517", "Sandbox.Text.0518", "Sandbox.Text.0519", "Sandbox.Text.0520",
		"Sandbox.Text.0521", "Sandbox.Text.0522", "Sandbox.Text.0523", "Sandbox.Text.0524", "Sandbox.Text.0525", "Sandbox.Text.0526", "Sandbox.Text.0527", "Sandbox.Text.0528", "Sandbox.Text.0529", "Sandbox.Text.0530",
		"Sandbox.Text.0531", "Sandbox.Text.0532", "Sandbox.Text.0533", "Sandbox.Text.0534", "Sandbox.Text.0535", "Sandbox.Text.0536", "Sandbox.Text.0537", "Sandbox.Text.0538", "Sandbox.Text.0539", "Sandbox.Text.0540",
		"Sandbox.Text.0541", "Sandbox.Text.0542", "Sandbox.Text.0543", "Sandbox.Text.0544", "Sandbox.Text.0545", "Sandbox.Text.0546", "Sandbox.Text.0547", "Sandbox.Text.0548", "Sandbox.Text.0549", "Sandbox.Text.0550",
		"Sandbox.Text.0551", "Sandbox.Text.0552", "Sandbox.Text.0553", "Sandbox.Text.0554", "Sandbox.Text.0555", "Sandbox.Text.0556", "Sandbox.Text.0557", "Sandbox.Text.0558", "Sandbox.Text.0559", "Sandbox.Text.0560",
		"Sandbox.Text.0561", "Sandbox.Text.0562", "Sandbox.Text.0563", "Sandbox.Text.0564", "Sandbox.Text.0565", "Sandbox.Text.0566", "Sandbox.Text.0567", "Sandbox.Text.0568", "Sandbox.Text.0569", "Sandbox.Text.0570",
		"Sandbox.Text.0571", "Sandbox.Text.0572", "Sandbox.Text.0573", "Sandbox.Text.0574", "Sandbox.Text.0575", "Sandbox.Text.0576", "Sandbox.Text.0577", "Sandbox.Text.0578", "Sandbox.Text.0579", "Sandbox.Text.0580",
		"Sandbox.Text.0581", "Sandbox.Text.0582", "Sandbox.Text.0583", "Sandbox.Text.0584", "Sandbox.Text.0585", "Sandbox.Text.0586", "Sandbox.Text.0587", "Sandbox.Text.0588", "Sandbox.Text.0589", "Sandbox.Text.0590",
		"Sandbox.Text.0591", "Sandbox.Text.0592", "Sandbox.Text.0593", "Sandbox.Text.0594", "Sandbox.Text.0595", "Sandbox.Text.0596", "Sandbox.Text.0597", "Sandbox.Text.0598", "Sandbox.Text.0599", "Sandbox.Text.0600",
		"Sandbox.Text.0601", "Sandbox.Text.0602", "Sandbox.Text.0603", "Sandbox.Text.0604", "Sandbox.Text.0605", "Sandbox.Text.0606", "Sandbox.Text.0607", "Sandbox.Text.0608", "Sandbox.Text.0609", "Sandbox.Text.0610",
		"Sandbox.Text.0611", "Sandbox.Text.0612", "Sandbox.Text.0613", "Sandbox.Text.0614", "Sandbox.Text.0615", "Sandbox.Text.0616", "Sandbox.Text.0617", "Sandbox.Text.0618", "Sandbox.Text.0619", "Sandbox.Text.0620",
		"Sandbox.Text.0621", "Sandbox.Text.0622", "Sandbox.Text.0623", "Sandbox.Text.0624", "Sandbox.Text.0625", "Sandbox.Text.0626", "Sandbox.Text.0627", "Sandbox.Text.0628", "Sandbox.Text.0629", "Sandbox.Text.0630",
		"Sandbox.Text.0631", "Sandbox.Text.0632", "Sandbox.Text.0633", "Sandbox.Text.0634", "Sandbox.Text.0635", "Sandbox.Text.0636", "Sandbox.Text.0637", "Sandbox.Text.0638", "Sandbox.Text.0639", "Sandbox.Text.0640",
		"Sandbox.Text.0641", "Sandbox.Text.0642", "Sandbox.Text.0643", "Sandbox.Text.0644", "Sandbox.Text.0645", "Sandbox.Text.0646", "Sandbox.Text.0647", "Sandbox.Text.0648", "Sandbox.Text.0649", "Sandbox.Text.0650",
		"Sandbox.Text.0651", "Sandbox.Text.0652", "Sandbox.Text.0653", "Sandbox.Text.0654", "Sandbox.Text.0655", "Sandbox.Text.0656", "Sandbox.Text.0657", "Sandbox.Text.0658", "Sandbox.Text.0659", "Sandbox.Text.0660",
		"Sandbox.Text.0661", "Sandbox.Text.0662", "Sandbox.Text.0663", "Sandbox.Text.0664", "Sandbox.Text.0665", "Sandbox.Text.0666", "Sandbox.Text.0667", "Sandbox.Text.0668", "Sandbox.Text.0669", "Sandbox.Text.0670",
		"Sandbox.Text.0671", "Sandbox.Text.0672", "Sandbox.Text.0673", "Sandbox.Text.0674", "Sandbox.Text.0675", "Sandbox.Text.0676", "Sandbox.Text.0677", "Sandbox.Text.0678", "Sandbox.Text.0679", "Sandbox.Text.0680",
		"Sandbox.Text.0681", "Sandbox.Text.0682", "Sandbox.Text.0683", "Sandbox.Text.0684", "Sandbox.Text.0685", "Sandbox.Text.0686", "Sandbox.Text.0687", "Sandbox.Text.0688", "Sandbox.Text.0689", "Sandbox.Text.0690",
		"Sandbox.Text.0691", "Sandbox.Text.0692", "Sandbox.Text.0693", "Sandbox.Text.0694", "Sandbox.Text.0695", "Sandbox.Text.0696", "Sandbox.Text.0697", "Sandbox.Text.0698", "Sandbox.Text.0699", "Sandbox.Text.0700",
		"Sandbox.Text.0701", "Sandbox.Text.0702", "Sandbox.Text.0703", "Sandbox.Text.0704", "Sandbox.Text.0705", "Sandbox.Text.0706", "Sandbox.Text.0707", "Sandbox.Text.0708", "Sandbox.Text.0709", "Sandbox.Text.0710",
		"Sandbox.Text.0711", "Sandbox.Text.0712", "Sandbox.Text.0713", "Sandbox.Text.0714", "Sandbox.Text.0715", "Sandbox.Text.0716", "Sandbox.Text.0717", "Sandbox.Text.0718", "Sandbox.Text.0719", "Sandbox.Text.0720",
		"Sandbox.Text.0721", "Sandbox.Text.0722", "Sandbox.Text.0723", "Sandbox.Text.0724", "Sandbox.Text.0725", "Sandbox.Text.0726", "Sandbox.Text.0727", "Sandbox.Text.0728", "Sandbox.Text.0729", "Sandbox.Text.0730",
		"Sandbox.Text.0731", "Sandbox.Text.0732", "Sandbox.Text.0733", "Sandbox.Text.0734", "Sandbox.Text.0735", "Sandbox.Text.0736", "Sandbox.Text.0737", "Sandbox.Text.0738", "Sandbox.Text.0739", "Sandbox.Text.0740",
		"Sandbox.Text.0741", "Sandbox.Text.0742", "Sandbox.Text.0743", "Sandbox.Text.0744", "Sandbox.Text.0745", "Sandbox.Text.0746", "Sandbox.Text.0747", "Sandbox.Text.0748", "Sandbox.Text.0749", "Sandbox.Text.0750",
		"Sandbox.Text.0751", "Sandbox.Text.0752", "Sandbox.Text.0753", "Sandbox.Text.0754", "Sandbox.Text.0755", "Sandbox.Text.0756", "Sandbox.Text.0757", "Sandbox.Text.0758", "Sandbox.Text.0759", "Sandbox.Text.0760",
		"Sandbox.Text.0761", "Sandbox.Text.0762", "Sandbox.Text.0763", "Sandbox.Text.0764", "Sandbox.Text.0765", "Sandbox.Text.0766", "Sandbox.Text.0767", "Sandbox.Text.0768", "Sandbox.Text.0769", "Sandbox.Text.0770",
		"Sandbox.Text.0771", "Sandbox.Text.0772", "Sandbox.Text.0773", "Sandbox.Text.0774", "Sandbox.Text.0775", "Sandbox.Text.0776", "Sandbox.Text.0777", "Sandbox.Text.0778", "Sandbox.Text.0779", "Sandbox.Text.0780",
		"Sandbox.Text.0781", "Sandbox.Text.0782", "Sandbox.Text.0783", "Sandbox.Text.0784", "Sandbox.Text.0785", "Sandbox.Text.0786", "Sandbox.Text.0787", "Sandbox.Text.0788", "Sandbox.Text.0789", "Sandbox.Text.0790",
		"Sandbox.Text.0791", "Sandbox.Text.0792", "Sandbox.Text.0793", "Sandbox.Text.0794", "Sandbox.Text.0795", "Sandbox.Text.0796", "Sandbox.Text.0797", "Sandbox.Text.0798", "Sandbox.Text.0799", "Sandbox.Text.0800",
		"Sandbox.Text.0801", "Sandbox.Text.0802", "Sandbox.Text.0803", "Sandbox.Text.0804", "Sandbox.Text.0805", "Sandbox.Text.0806", "Sandbox.Text.0807", "Sandbox.Text.0808", "Sandbox.Text.0809", "Sandbox.Text.0810",
		"Sandbox.Text.0811", "Sandbox.Text.0812", "Sandbox.Text.0813", "Sandbox.Text.0814", "Sandbox.Text.0815", "Sandbox.Text.0816", "Sandbox.Text.0817", "Sandbox.Text.0818", "Sandbox.Text.0819", "Sandbox.Text.0820",
		"Sandbox.Text.0821", "Sandbox.Text.0822", "Sandbox.Text.0823", "Sandbox.Text.0824", "Sandbox.Text.0825", "Sandbox.Text.0826", "Sandbox.Text.0827", "Sandbox.Text.0828", "Sandbox.Text.0829", "Sandbox.Text.0830",
		"Sandbox.Text.0831", "Sandbox.Text.0832", "Sandbox.Text.0833", "Sandbox.Text.0834", "Sandbox.Text.0835", "Sandbox.Text.0836", "Sandbox.Text.0837", "Sandbox.Text.0838", "Sandbox.Text.0839", "Sandbox.Text.0840",
		"Sandbox.Text.0841", "Sandbox.Text.0842", "Sandbox.Text.0843", "Sandbox.Text.0844", "Sandbox.Text.0845", "Sandbox.Text.0846", "Sandbox.Text.0847", "Sandbox.Text.0848", "Sandbox.Text.0849", "Sandbox.Text.0850",
		"Sandbox.Text.0851", "Sandbox.Text.0852", "Sandbox.Text.0853", "Sandbox.Text.0854", "Sandbox.Text.0855", "Sandbox.Text.0856", "Sandbox.Text.0857", "Sandbox.Text.0858", "Sandbox.Text.0859", "Sandbox.Text.0860",
		"Sandbox.Text.0861", "Sandbox.Text.0862", "Sandbox.Text.0863", "Sandbox.Text.0864", "Sandbox.Text.0865", "Sandbox.Text.0866", "Sandbox.Text.0867", "Sandbox.Text.0868", "Sandbox.Text.0869", "Sandbox.Text.0870",
		"Sandbox.Text.0871", "Sandbox.Text.0872", "Sandbox.Text.0873", "Sandbox.Text.0874", "Sandbox.Text.0875", "Sandbox.Text.0876", "Sandbox.Text.0877", "Sandbox.Text.0878", "Sandbox.Text.0879", "Sandbox.Text.0880",
		"Sandbox.Text.0881", "Sandbox.Text.0882", "Sandbox.Text.0883", "Sandbox.Text.0884", "Sandbox.Text.0885", "Sandbox.Text.0886", "Sandbox.Text.0887", "Sandbox.Text.0888", "Sandbox.Text.0889", "Sandbox.Text.0890",
		"Sandbox.Text.0891", "Sandbox.Text.0892", "Sandbox.Text.0893", "Sandbox.Text.0894", "Sandbox.Text.0895", "Sandbox.Text.0896", "Sandbox.Text.0897", "Sandbox.Text.0898", "Sandbox.Text.0899", "Sandbox.Text.0900",
		"Sandbox.Text.0901", "Sandbox.Text.0902", "Sandbox.Text.0903", "Sandbox.Text.0904", "Sandbox.Text.0905", "Sandbox.Text.0906", "Sandbox.Text.0907", "Sandbox.Text.0908", "Sandbox.Text.0909", "Sandbox.Text.0910",
		"Sandbox.Text.0911", "Sandbox.Text.0912", "Sandbox.Text.0913", "Sandbox.Text.0914", "Sandbox.Text.0915", "Sandbox.Text.0916", "Sandbox.Text.0917", "Sandbox.Text.0918", "Sandbox.Text.0919", "Sandbox.Text.0920",
		"Sandbox.Text.0921", "Sandbox.Text.0922", "Sandbox.Text.0923", "Sandbox.Text.0924", "Sandbox.Text.0925", "Sandbox.Text.0926", "Sandbox.Text.0927", "Sandbox.Text.0928", "Sandbox.Text.0929", "Sandbox.Text.0930",
		"Sandbox.Text.0931", "Sandbox.Text.0932", "Sandbox.Text.0933", "Sandbox.Text.0934", "Sandbox.Text.0935", "Sandbox.Text.0936", "Sandbox.Text.0937", "Sandbox.Text.0938", "Sandbox.Text.0939", "Sandbox.Text.0940",
		"Sandbox.Text.0941", "Sandbox.Text.0942", "Sandbox.Text.0943", "Sandbox.Text.0944", "Sandbox.Text.0945", "Sandbox.Text.0946", "Sandbox.Text.0947", "Sandbox.Text.0948", "Sandbox.Text.0949", "Sandbox.Text.0950",
		"Sandbox.Text.0951", "Sandbox.Text.0952", "Sandbox.Text.0953", "Sandbox.Text.0954", "Sandbox.Text.0955", "Sandbox.Text.0956", "Sandbox.Text.0957", "Sandbox.Text.0958", "Sandbox.Text.0959", "Sandbox.Text.0960",
		"Sandbox.Text.0961", "Sandbox.Text.0962", "Sandbox.Text.0963", "Sandbox.Text.0964", "Sandbox.Text.0965", "Sandbox.Text.0966", "Sandbox.Text.0967", "Sandbox.Text.0968", "Sandbox.Text.0969", "Sandbox.Text.0970",
		"Sandbox.Text.0971", "Sandbox.Text.0972", "Sandbox.Text.0973", "Sandbox.Text.0974", "Sandbox.Text.0975", "Sandbox.Text.0976", "Sandbox.Text.0977", "Sandbox.Text.0978", "Sandbox.Text.0979", "Sandbox.Text.0980",
		"Sandbox.Text.0981", "Sandbox.Text.0982", "Sandbox.Text.0983", "Sandbox.Text.0984", "Sandbox.Text.0985", "Sandbox.Text.0986", "Sandbox.Text.0987", "Sandbox.Text.0988", "Sandbox.Text.0989", "Sandbox.Text.0990",
		"Sandbox.Text.0991", "Sandbox.Text.0992", "Sandbox.Text.0993", "Sandbox.Text.0994", "Sandbox.Text.0995", "Sandbox.Text.0996", "Sandbox.Text.0997", "Sandbox.Text.0998", "Sandbox.Text.0999", "Sandbox.Text.1000",
		"Sandbox.Text.1001", "Sandbox.Text.1002", "Sandbox.Text.1003", "Sandbox.Text.1004", "Sandbox.Text.1005", "Sandbox.Text.1006", "Sandbox.Text.1007", "Sandbox.Text.1008", "Sandbox.Text.1009", "Sandbox.Text.1010",
		"Sandbox.Text.1011", "Sandbox.Text.1012", "Sandbox.Text.1013", "Sandbox.Text.1014", "Sandbox.Text.1015", "Sandbox.Text.1016", "Sandbox.Text.1017", "Sandbox.Text.1018", "Sandbox.Text.1019", "Sandbox.Text.1020",
		"Sandbox.Text.1021", "Sandbox.Text.1022", "Sandbox.Text.1023", "Sandbox.Text.1024", "Sandbox.Text.1025", "Sandbox.Text.1026", "Sandbox.Text.1027", "Sandbox.Text.1028", "Sandbox.Text.1029", "Sandbox.Text.1030",
		"Sandbox.Text.1031", "Sandbox.Text.1032", "Sandbox.Text.1033", "Sandbox.Text.1034", "Sandbox.Text.1035", "Sandbox.Text.1036", "Sandbox.Text.1037", "Sandbox.Text.1038", "Sandbox.Text.1039", "Sandbox.Text.1040",
		"Sandbox.Text.1041", "Sandbox.Text.1042", "Sandbox.Text.1043", "Sandbox.Text.1044", "Sandbox.Text.1045", "Sandbox.Text.1046", "Sandbox.Text.1047", "Sandbox.Text.1048", "Sandbox.Text.1049", "Sandbox.Text.1050",
		"Sandbox.Text.1051", "Sandbox.Text.1052", "Sandbox.Text.1053", "Sandbox.Text.1054", "Sandbox.Text.1055", "Sandbox.Text.1056", "Sandbox.Text.1057", "Sandbox.Text.1058", "Sandbox.Text.1059", "Sandbox.Text.1060",
		"Sandbox.Text.1061", "Sandbox.Text.1062", "Sandbox.Text.1063", "Sandbox.Text.1064", "Sandbox.Text.1065", "Sandbox.Text.1066", "Sandbox.Text.1067", "Sandbox.Text.1068", "Sandbox.Text.1069", "Sandbox.Text.1070",
		"Sandbox.Text.1071", "Sandbox.Text.1072", "Sandbox.Text.1073", "Sandbox.Text.1074", "Sandbox.Text.1075", "Sandbox.Text.1076", "Sandbox.Text.1077", "Sandbox.Text.1078", "Sandbox.Text.1079", "Sandbox.Text.1080",
		"Sandbox.Text.1081", "Sandbox.Text.1082", "Sandbox.Text.1083", "Sandbox.Text.1084", "Sandbox.Text.1085", "Sandbox.Text.1086", "Sandbox.Text.1087", "Sandbox.Text.1088", "Sandbox.Text.1089", "Sandbox.Text.1090",
		"Sandbox.Text.1091", "Sandbox.Text.1092", "Sandbox.Text.1093", "Sandbox.Text.1094", "Sandbox.Text.1095", "Sandbox.Text.1096", "Sandbox.Text.1097", "Sandbox.Text.1098", "Sandbox.Text.1099", "Sandbox.Text.1100",
		"Sandbox.Text.1101", "Sandbox.Text.1102", "Sandbox.Text.1103", "Sandbox.Text.1104", "Sandbox.Text.1105", "Sandbox.Text.1106", "Sandbox.Text.1107", "Sandbox.Text.1108", "Sandbox.Text.1109", "Sandbox.Text.1110",
		"Sandbox.Text.1111", "Sandbox.Text.1112", "Sandbox.Text.1113", "Sandbox.Text.1114", "Sandbox.Text.1115", "Sandbox.Text.1116", "Sandbox.Text.1117", "Sandbox.Text.1118", "Sandbox.Text.1119", "Sandbox.Text.1120",
		"Sandbox.Text.1121", "Sandbox.Text.1122", "Sandbox.Text.1123", "Sandbox.Text.1124", "Sandbox.Text.1125", "Sandbox.Text.1126", "Sandbox.Text.1127", "Sandbox.Text.1128", "Sandbox.Text.1129", "Sandbox.Text.1130",
		"Sandbox.Text.1131", "Sandbox.Text.1132", "Sandbox.Text.1133", "Sandbox.Text.1134", "Sandbox.Text.1135", "Sandbox.Text.1136", "Sandbox.Text.1137", "Sandbox.Text.1138", "Sandbox.Text.1139", "Sandbox.Text.1140",
		"Sandbox.Text.1141", "Sandbox.Text.1142", "Sandbox.Text.1143", "Sandbox.Text.1144", "Sandbox.Text.1145", "Sandbox.Text.1146", "Sandbox.Text.1147", "Sandbox.Text.1148", "Sandbox.Text.1149", "Sandbox.Text.1150",
		"Sandbox.Text.1151", "Sandbox.Text.1152", "Sandbox.Text.1153", "Sandbox.Text.1154", "Sandbox.Text.1155", "Sandbox.Text.1156", "Sandbox.Text.1157", "Sandbox.Text.1158", "Sandbox.Text.1159", "Sandbox.Text.1160",
		"Sandbox.Text.1161", "Sandbox.Text.1162", "Sandbox.Text.1163", "Sandbox.Text.1164", "Sandbox.Text.1165", "Sandbox.Text.1166", "Sandbox.Text.1167", "Sandbox.Text.1168", "Sandbox.Text.1169", "Sandbox.Text.1170",
		"Sandbox.Text.1171", "Sandbox.Text.1172", "Sandbox.Text.1173", "Sandbox.Text.1174", "Sandbox.Text.1175", "Sandbox.Text.1176", "Sandbox.Text.1177", "Sandbox.Text.1178", "Sandbox.Text.1179", "Sandbox.Text.1180",
		"Sandbox.Text.1181", "Sandbox.Text.1182", "Sandbox.Text.1183", "Sandbox.Text.1184", "Sandbox.Text.1185", "Sandbox.Text.1186", "Sandbox.Text.1187", "Sandbox.Text.1188", "Sandbox.Text.1189", "Sandbox.Text.1190",
		"Sandbox.Text.1191", "Sandbox.Text.1192", "Sandbox.Text.1193", "Sandbox.Text.1194", "Sandbox.Text.1195", "Sandbox.Text.1196", "Sandbox.Text.1197", "Sandbox.Text.1198", "Sandbox.Text.1199", "Sandbox.Text.1200",
		"Sandbox.Text.1201", "Sandbox.Text.1202", "Sandbox.Text.1203", "Sandbox.Text.1204", "Sandbox.Text.1205", "Sandbox.Text.1206", "Sandbox.Text.1207", "Sandbox.Text.1208", "Sandbox.Text.1209", "Sandbox.Text.1210",
		"Sandbox.Text.1211", "Sandbox.Text.1212", "Sandbox.Text.1213", "Sandbox.Text.1214", "Sandbox.Text.1215", "Sandbox.Text.1216", "Sandbox.Text.1217", "Sandbox.Text.1218", "Sandbox.Text.1219", "Sandbox.Text.1220",
		"Sandbox.Text.1221", "Sandbox.Text.1222", "Sandbox.Text.1223", "Sandbox.Text.1224", "Sandbox.Text.1225", "Sandbox.Text.1226", "Sandbox.Text.1227", "Sandbox.Text.1228", "Sandbox.Text.1229", "Sandbox.Text.1230",
		"Sandbox.Text.1231", "Sandbox.Text.1232", "Sandbox.Text.1233", "Sandbox.Text.1234", "Sandbox.Text.1235", "Sandbox.Text.1236", "Sandbox.Text.1237", "Sandbox.Text.1238", "Sandbox.Text.1239", "Sandbox.Text.1240",
		"Sandbox.Text.1241", "Sandbox.Text.1242", "Sandbox.Text.1243", "Sandbox.Text.1244", "Sandbox.Text.1245", "Sandbox.Text.1246", "Sandbox.Text.1247", "Sandbox.Text.1248", "Sandbox.Text.1249", "Sandbox.Text.1250",
		"Sandbox.Text.1251", "Sandbox.Text.1252", "Sandbox.Text.1253", "Sandbox.Text.1254", "Sandbox.Text.1255", "Sandbox.Text.1256", "Sandbox.Text.1257", "Sandbox.Text.1258", "Sandbox.Text.1259", "Sandbox.Text.1260",
		"Sandbox.Text.1261", "Sandbox.Text.1262", "Sandbox.Text.1263", "Sandbox.Text.1264", "Sandbox.Text.1265", "Sandbox.Text.1266", "Sandbox.Text.1267", "Sandbox.Text.1268", "Sandbox.Text.1269", "Sandbox.Text.1270",
		"Sandbox.Text.1271", "Sandbox.Text.1272", "Sandbox.Text.1273", "Sandbox.Text.1274", "Sandbox.Text.1275", "Sandbox.Text.1276", "Sandbox.Text.1277", "Sandbox.Text.1278", "Sandbox.Text.1279", "Sandbox.Text.1280",
		"Sandbox.Text.1281", "Sandbox.Text.1282", "Sandbox.Text.1283", "Sandbox.Text.1284", "Sandbox.Text.1285", "Sandbox.Text.1286", "Sandbox.Text.1287", "Sandbox.Text.1288", "Sandbox.Text.1289", "Sandbox.Text.1290",
		"Sandbox.Text.1291", "Sandbox.Text.1292", "Sandbox.Text.1293", "Sandbox.Text.1294", "Sandbox.Text.1295", "Sandbox.Text.1296", "Sandbox.Text.1297", "Sandbox.Text.1298", "Sandbox.Text.1299", "Sandbox.Text.1300",
		"Sandbox.Text.1301", "Sandbox.Text.1302", "Sandbox.Text.1303", "Sandbox.Text.1304", "Sandbox.Text.1305", "Sandbox.Text.1306", "Sandbox.Text.1307", "Sandbox.Text.1308", "Sandbox.Text.1309", "Sandbox.Text.1310",
		"Sandbox.Text.1311", "Sandbox.Text.1312", "Sandbox.Text.1313", "Sandbox.Text.1314", "Sandbox.Text.1315", "Sandbox.Text.1316", "Sandbox.Text.1317", "Sandbox.Text.1318", "Sandbox.Text.1319", "Sandbox.Text.1320",
		"Sandbox.Text.1321", "Sandbox.Text.1322", "Sandbox.Text.1323", "Sandbox.Text.1324", "Sandbox.Text.1325",
		"Sandbox.Text.1326", "Sandbox.Text.1327", "Sandbox.Text.1328", "Sandbox.Text.1329", "Sandbox.Text.1330", "Sandbox.Text.1331", "Sandbox.Text.1332", "Sandbox.Text.1333", "Sandbox.Text.1334", "Sandbox.Text.1335", 
		"Sandbox.Text.1336", "Sandbox.Text.1337", "Sandbox.Text.1338", "Sandbox.Text.1339", "Sandbox.Text.1340", "Sandbox.Text.1341", "Sandbox.Text.1342", "Sandbox.Text.1343", "Sandbox.Text.1344", "Sandbox.Text.1345", 
		"Sandbox.Text.1346", "Sandbox.Text.1347", "Sandbox.Text.1348", "Sandbox.Text.1349", "Sandbox.Text.1350", "Sandbox.Text.1351", "Sandbox.Text.1352", "Sandbox.Text.1353", "Sandbox.Text.1354", "Sandbox.Text.1355", 
		"Sandbox.Text.1356", "Sandbox.Text.1357", "Sandbox.Text.1358", "Sandbox.Text.1359", "Sandbox.Text.1360", "Sandbox.Text.1361", "Sandbox.Text.1362", "Sandbox.Text.1363", "Sandbox.Text.1364"
	};

	public static void Apply(object? root)
	{
		if (root != null)
		{
			ApplyObject(root, new HashSet<object>(ReferenceEqualityComparer.Instance));
		}
	}

	public static string Localize(string text)
	{
		string? key;
		string localized;
		return (Lookup.Value.TryGetValue(text, out key) && LuminaLocalization.TryGet(key, out localized)) ? localized : text;
	}

	public static string Format(string format, params object?[] args)
	{
		return string.Format(LuminaLocalization.CurrentCulture, Localize(format), args);
	}

	private static void ApplyObject(object target, HashSet<object> visited)
	{
		if (!visited.Add(target))
		{
			return;
		}
		if (target is Control control)
		{
			if (control.Classes.Contains("NoSandboxTextLocalization"))
			{
				return;
			}
			ApplyKnownTextProperties(target);
			ApplyTooltip(control);
		}
		else
		{
			ApplyKnownTextProperties(target);
		}
		if (target is ILogical logical)
		{
			foreach (ILogical child in logical.LogicalChildren)
			{
				ApplyObject(child, visited);
			}
		}
		ApplyKnownChildProperties(target, visited);
	}

	private static void ApplyKnownTextProperties(object target)
	{
		if (!(target is TextBlock textBlock))
		{
			if (!(target is TextBox textBox))
			{
				if (target is ContentControl contentControl)
				{
					contentControl.Content = LocalizeObject(contentControl.Content);
				}
			}
			else
			{
				textBox.Text = LocalizeNullableString(textBox.Text);
				textBox.PlaceholderText = LocalizeNullableString(textBox.PlaceholderText);
			}
		}
		else
		{
			textBlock.Text = LocalizeNullableString(textBlock.Text);
		}
		if (!(target is MenuItem menuItem))
		{
			if (!(target is LuminaDialog dialog))
			{
				if (!(target is LuminaColorSwatch swatch))
				{
					if (!(target is LuminaDescriptionsItem descriptionsItem))
					{
						if (!(target is LuminaFormGroup formGroup))
						{
							if (!(target is LuminaFormItem formItem))
							{
								if (!(target is LuminaGroupBox groupBox))
								{
									if (!(target is LuminaListItem listItem))
									{
										if (!(target is LuminaNavigationItem navigationItem))
										{
											if (!(target is LuminaMultiSelect multiSelect))
											{
												if (!(target is LuminaSectionBase section))
												{
													if (!(target is LuminaSettingsCard settingsCard))
													{
														if (!(target is LuminaSettingItem settingItem))
														{
															if (!(target is LuminaSettingsOption settingsOption))
															{
																if (!(target is LuminaShell shell))
																{
																	if (target is LuminaTagInput tagInput)
																	{
																		tagInput.Watermark = LocalizeNullableString(tagInput.Watermark);
																	}
																}
																else
																{
																	shell.Title = LocalizeObject(shell.Title);
																}
															}
															else
															{
																settingsOption.Header = LocalizeNullableString(settingsOption.Header);
																settingsOption.Description = LocalizeNullableString(settingsOption.Description);
																settingsOption.PlaceholderText = LocalizeNullableString(settingsOption.PlaceholderText);
																settingsOption.Value = LocalizeObject(settingsOption.Value);
															}
														}
														else
														{
															settingItem.Header = LocalizeNullableString(settingItem.Header);
															settingItem.Description = LocalizeNullableString(settingItem.Description);
														}
													}
													else
													{
														settingsCard.Header = LocalizeNullableString(settingsCard.Header);
														settingsCard.Description = LocalizeNullableString(settingsCard.Description);
													}
												}
												else
												{
													section.Header = LocalizeNullableString(section.Header);
												}
											}
											else
											{
												multiSelect.Watermark = LocalizeNullableString(multiSelect.Watermark);
											}
										}
										else
										{
											navigationItem.Header = LocalizeObject(navigationItem.Header);
										}
									}
									else
									{
										listItem.Header = LocalizeNullableString(listItem.Header);
										listItem.Description = LocalizeNullableString(listItem.Description);
										listItem.Value = LocalizeNullableString(listItem.Value);
									}
								}
								else
								{
									groupBox.Header = LocalizeObject(groupBox.Header);
									groupBox.Description = LocalizeNullableString(groupBox.Description);
								}
							}
							else
							{
								formItem.Label = LocalizeObject(formItem.Label);
								formItem.Description = LocalizeNullableString(formItem.Description);
							}
						}
						else
						{
							formGroup.Description = LocalizeNullableString(formGroup.Description);
						}
					}
					else
					{
						descriptionsItem.Label = LocalizeObject(descriptionsItem.Label);
					}
				}
				else
				{
					swatch.Title = LocalizeNullableString(swatch.Title);
					swatch.Description = LocalizeNullableString(swatch.Description);
				}
			}
			else
			{
				dialog.Title = LocalizeNullableString(dialog.Title);
			}
		}
		else
		{
			menuItem.Header = LocalizeObject(menuItem.Header);
		}
	}

	private static string? LocalizeNullableString(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? value : Localize(value);
	}

	private static object? LocalizeObject(object? value)
	{
		return (value is string text && !string.IsNullOrWhiteSpace(text)) ? Localize(text) : value;
	}

	private static void ApplyTooltip(Control control)
	{
		if (ToolTip.GetTip(control) is string tip && !string.IsNullOrWhiteSpace(tip))
		{
			string localized = Localize(tip);
			if (localized != tip)
			{
				ToolTip.SetTip(control, localized);
			}
		}
	}

	private static void ApplyKnownChildProperties(object target, HashSet<object> visited)
	{
		if (!(target is MenuItem menuItem))
		{
			if (!(target is LuminaDescriptionsItem descriptionsItem))
			{
				if (!(target is LuminaFormItem formItem))
				{
					if (!(target is LuminaGroupBox groupBox))
					{
						if (!(target is LuminaNavigationItem navigationItem))
						{
							if (!(target is LuminaSettingsOption settingsOption))
							{
								if (!(target is LuminaShell shell))
								{
									if (!(target is ItemsControl itemsControl))
									{
										if (target is ContentControl contentControl)
										{
											ApplyChildValue(contentControl.Content, visited);
										}
									}
									else
									{
										ApplyChildValue(itemsControl.Items, visited);
									}
								}
								else
								{
									ApplyChildValue(shell.Title, visited);
									ApplyChildValue(shell.DialogContent, visited);
									ApplyChildValue(shell.BottomSheetContent, visited);
								}
							}
							else
							{
								ApplyChildValue(settingsOption.Icon, visited);
								ApplyChildValue(settingsOption.Value, visited);
								ApplyChildValue(settingsOption.TrailingContent, visited);
								ApplyChildValue(settingsOption.SelectItemsSource, visited);
							}
						}
						else
						{
							ApplyChildValue(navigationItem.Header, visited);
						}
					}
					else
					{
						ApplyChildValue(groupBox.Header, visited);
					}
				}
				else
				{
					ApplyChildValue(formItem.Label, visited);
				}
			}
			else
			{
				ApplyChildValue(descriptionsItem.Label, visited);
			}
		}
		else
		{
			ApplyChildValue(menuItem.Header, visited);
			ApplyChildValue(menuItem.Items, visited);
		}
	}

	private static void ApplyChildValue(object? value, HashSet<object> visited)
	{
		if (value == null || value is string)
		{
			return;
		}
		if (value is IEnumerable enumerable)
		{
			{
				foreach (object item in enumerable)
				{
					ApplyChildValue(item, visited);
				}
				return;
			}
		}
		if ((value is AvaloniaObject || value is ILogical) ? true : false)
		{
			ApplyObject(value, visited);
		}
	}

	private static Dictionary<string, string> CreateLookup()
	{
		Dictionary<string, string> lookup = new Dictionary<string, string>(StringComparer.Ordinal);
		string[] textKeys = TextKeys;
		foreach (string key in textKeys)
		{
			foreach (CultureInfo culture in LuminaLocalization.SupportedCultures)
			{
				AddLocalizedValue(lookup, key, culture);
			}
			AddLocalizedValue(lookup, key, LuminaLocalization.FallbackCulture);
			AddLocalizedValue(lookup, key, CultureInfo.InvariantCulture);
		}
		return lookup;
	}

	private static void AddLocalizedValue(Dictionary<string, string> lookup, string key, CultureInfo culture)
	{
		if (LuminaLocalization.TryGet(key, culture, out string value) && !string.IsNullOrWhiteSpace(value))
		{
			lookup[value] = key;
		}
	}
}
