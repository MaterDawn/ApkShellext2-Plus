using ApkShellext2;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Media = System.Windows.Media;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using WebPWrapper;

namespace ApkQuickReader
{
    /// <summary>
    /// from ResourceTypes.h
    /// </summary>
    public enum RES_TYPE : UInt16
    {
        RES_NULL_TYPE = 0x0000,
        RES_STRING_POOL_TYPE = 0x0001,
        RES_TABLE_TYPE = 0x0002,
        RES_XML_TYPE = 0x0003,

        // Chunk types in RES_XML_TYPE
        RES_XML_FIRST_CHUNK_TYPE = 0x0100,
        RES_XML_START_NAMESPACE_TYPE = 0x0100,
        RES_XML_END_NAMESPACE_TYPE = 0x0101,
        RES_XML_START_ELEMENT_TYPE = 0x0102,
        RES_XML_END_ELEMENT_TYPE = 0x0103,
        RES_XML_CDATA_TYPE = 0x0104,
        RES_XML_LAST_CHUNK_TYPE = 0x017f,
        // This contains a uint32_t array mapping strings in the string
        // pool back to resource identifiers.  It is optional.
        RES_XML_RESOURCE_MAP_TYPE = 0x0180,

        // Chunk types in RES_TABLE_TYPE
        RES_TABLE_PACKAGE_TYPE = 0x0200,
        RES_TABLE_TYPE_TYPE = 0x0201,
        RES_TABLE_TYPE_SPEC_TYPE = 0x0202
    };

    /// <summary>
    /// from ResourceTypes.h
    /// </summary>
    public enum DATA_TYPE : byte
    {
        // Contains no data.
        TYPE_NULL = 0x00,
        // The 'data' holds an attribute resource identifier.
        TYPE_ATTRIBUTE = 0x02,
        // The 'data' holds a single-precision floating point number.
        TYPE_FLOAT = 0x04,
        // The 'data' holds a complex number encoding a dimension value,
        // such as "100in".
        TYPE_DIMENSION = 0x05,
        // The 'data' holds a complex number encoding a fraction of a
        // container.
        TYPE_FRACTION = 0x06,
        // The 'data' is a raw integer value of the form n..n.
        TYPE_INT_DEC = 0x10,
        // The 'data' is a raw integer value of the form 0xn..n.
        TYPE_INT_HEX = 0x11,
        // The 'data' is either 0 or 1, for input "false" or "true" respectively.
        TYPE_INT_BOOLEAN = 0x12,
        // The 'data' is a raw integer value of the form #aarrggbb.
        TYPE_INT_COLOR_ARGB8 = 0x1c,
        // The 'data' is a raw integer value of the form #rrggbb.
        TYPE_INT_COLOR_RGB8 = 0x1d,
        // The 'data' is a raw integer value of the form #argb.
        TYPE_INT_COLOR_ARGB4 = 0x1e,
        // The 'data' is a raw integer value of the form #rgb.
        TYPE_INT_COLOR_RGB4 = 0x1f,
        TYPE_REFERENCE = 0x01,
        TYPE_STRING = 0x03,
        // The 'data' is a dynamic reference (aapt2 --shared-lib / runtime resource overlay)
        TYPE_DYNAMIC_REFERENCE = 0x07,
        // The 'data' is a dynamic attribute reference
        TYPE_DYNAMIC_ATTRIBUTE = 0x08,
    }

    /// <summary>
    /// from ResourceTypes.h
    /// </summary>
    enum COMPLEX_TYPE : UInt32
    {
        // Where the unit type information is.  This gives us 16 possible
        // types, as defined below.
        COMPLEX_UNIT_SHIFT = 0,
        COMPLEX_UNIT_MASK = 0xf,
        // TYPE_DIMENSION: Value is raw pixels.
        COMPLEX_UNIT_PX = 0,
        // TYPE_DIMENSION: Value is Device Independent Pixels.
        COMPLEX_UNIT_DIP = 1,
        // TYPE_DIMENSION: Value is a Scaled device independent Pixels.
        COMPLEX_UNIT_SP = 2,
        // TYPE_DIMENSION: Value is in points.
        COMPLEX_UNIT_PT = 3,
        // TYPE_DIMENSION: Value is in inches.
        COMPLEX_UNIT_IN = 4,
        // TYPE_DIMENSION: Value is in millimeters.
        COMPLEX_UNIT_MM = 5,
        // TYPE_FRACTION: A basic fraction of the overall size.
        COMPLEX_UNIT_FRACTION = 0,
        // TYPE_FRACTION: A fraction of the parent size.
        COMPLEX_UNIT_FRACTION_PARENT = 1,
        // Where the radix information is, telling where the decimal place
        // appears in the mantissa.  This give us 4 possible fixed point
        // representations as defined below.
        COMPLEX_RADIX_SHIFT = 4,
        COMPLEX_RADIX_MASK = 0x3,
        // The mantissa is an integral number -- i.e., 0xnnnnnn.0
        COMPLEX_RADIX_23p0 = 0,
        // The mantissa magnitude is 16 bits -- i.e, 0xnnnn.nn
        COMPLEX_RADIX_16p7 = 1,
        // The mantissa magnitude is 8 bits -- i.e, 0xnn.nnnn
        COMPLEX_RADIX_8p15 = 2,
        // The mantissa magnitude is 0 bits -- i.e, 0x0.nnnnnn
        COMPLEX_RADIX_0p23 = 3,
        // Where the actual value is.  This gives us 23 bits of
        // precision.  The top bit is the sign.
        COMPLEX_MANTISSA_SHIFT = 8,
        COMPLEX_MANTISSA_MASK = 0xffffff
    };

    public enum R_attr : uint
    {
        absListViewStyle = 16842858, // 0x101006a
        accessibilityEventTypes = 16843648, // 0x1010380
        accessibilityFeedbackType = 16843650, // 0x1010382
        accessibilityFlags = 16843652, // 0x1010384
        accountPreferences = 16843423, // 0x101029f
        accountType = 16843407, // 0x101028f
        action = 16842797, // 0x101002d
        actionBarDivider = 16843675, // 0x101039b
        actionBarItemBackground = 16843676, // 0x101039c
        actionBarSize = 16843499, // 0x10102eb
        actionBarSplitStyle = 16843656, // 0x1010388
        actionBarStyle = 16843470, // 0x10102ce
        actionBarTabBarStyle = 16843508, // 0x10102f4
        actionBarTabStyle = 16843507, // 0x10102f3
        actionBarTabTextStyle = 16843509, // 0x10102f5
        actionBarWidgetTheme = 16843671, // 0x1010397
        actionButtonStyle = 16843480, // 0x10102d8
        actionDropDownStyle = 16843479, // 0x10102d7
        actionLayout = 16843515, // 0x10102fb
        actionMenuTextAppearance = 16843616, // 0x1010360
        actionMenuTextColor = 16843617, // 0x1010361
        actionModeBackground = 16843483, // 0x10102db
        actionModeCloseButtonStyle = 16843511, // 0x10102f7
        actionModeCloseDrawable = 16843484, // 0x10102dc
        actionModeCopyDrawable = 16843538, // 0x1010312
        actionModeCutDrawable = 16843537, // 0x1010311
        actionModePasteDrawable = 16843539, // 0x1010313
        actionModeSelectAllDrawable = 16843646, // 0x101037e
        actionModeSplitBackground = 16843677, // 0x101039d
        actionModeStyle = 16843668, // 0x1010394
        actionOverflowButtonStyle = 16843510, // 0x10102f6
        actionProviderClass = 16843657, // 0x1010389
        actionViewClass = 16843516, // 0x10102fc
        activatedBackgroundIndicator = 16843517, // 0x10102fd
        activityCloseEnterAnimation = 16842938, // 0x10100ba
        activityCloseExitAnimation = 16842939, // 0x10100bb
        activityOpenEnterAnimation = 16842936, // 0x10100b8
        activityOpenExitAnimation = 16842937, // 0x10100b9
        addStatesFromChildren = 16842992, // 0x10100f0
        adjustViewBounds = 16843038, // 0x101011e
        alertDialogIcon = 16843605, // 0x1010355
        alertDialogStyle = 16842845, // 0x101005d
        alertDialogTheme = 16843529, // 0x1010309
        alignmentMode = 16843642, // 0x101037a
        allContactsName = 16843468, // 0x10102cc
        allowBackup = 16843392, // 0x1010280
        allowClearUserData = 16842757, // 0x1010005
        allowParallelSyncs = 16843570, // 0x1010332
        allowSingleTap = 16843353, // 0x1010259
        allowTaskReparenting = 16843268, // 0x1010204
        alpha = 16843551, // 0x101031f
        alphabeticShortcut = 16843235, // 0x10101e3
        alwaysDrawnWithCache = 16842991, // 0x10100ef
        alwaysRetainTaskState = 16843267, // 0x1010203
        angle = 16843168, // 0x10101a0
        animateFirstView = 16843477, // 0x10102d5
        animateLayoutChanges = 16843506, // 0x10102f2
        animateOnClick = 16843356, // 0x101025c
        animation = 16843213, // 0x10101cd
        animationCache = 16842989, // 0x10100ed
        animationDuration = 16843026, // 0x1010112
        animationOrder = 16843214, // 0x10101ce
        animationResolution = 16843546, // 0x101031a
        antialias = 16843034, // 0x101011a
        anyDensity = 16843372, // 0x101026c
        apiKey = 16843281, // 0x1010211
        author = 16843444, // 0x10102b4
        authorities = 16842776, // 0x1010018
        autoAdvanceViewId = 16843535, // 0x101030f
        autoCompleteTextViewStyle = 16842859, // 0x101006b
        autoLink = 16842928, // 0x10100b0
        autoStart = 16843445, // 0x10102b5
        autoText = 16843114, // 0x101016a
        autoUrlDetect = 16843404, // 0x101028c
        background = 16842964, // 0x10100d4
        backgroundDimAmount = 16842802, // 0x1010032
        backgroundDimEnabled = 16843295, // 0x101021f
        backgroundSplit = 16843659, // 0x101038b
        backgroundStacked = 16843658, // 0x101038a
        backupAgent = 16843391, // 0x101027f
        baseline = 16843548, // 0x101031c
        baselineAlignBottom = 16843042, // 0x1010122
        baselineAligned = 16843046, // 0x1010126
        baselineAlignedChildIndex = 16843047, // 0x1010127
        borderlessButtonStyle = 16843563, // 0x101032b
        bottom = 16843184, // 0x10101b0
        bottomBright = 16842957, // 0x10100cd
        bottomDark = 16842953, // 0x10100c9
        bottomLeftRadius = 16843179, // 0x10101ab
        bottomMedium = 16842958, // 0x10100ce
        bottomOffset = 16843351, // 0x1010257
        bottomRightRadius = 16843180, // 0x10101ac
        breadCrumbShortTitle = 16843524, // 0x1010304
        breadCrumbTitle = 16843523, // 0x1010303
        bufferType = 16843086, // 0x101014e
        button = 16843015, // 0x1010107
        buttonBarButtonStyle = 16843567, // 0x101032f
        buttonBarStyle = 16843566, // 0x101032e
        buttonStyle = 16842824, // 0x1010048
        buttonStyleInset = 16842826, // 0x101004a
        buttonStyleSmall = 16842825, // 0x1010049
        buttonStyleToggle = 16842827, // 0x101004b
        cacheColorHint = 16843009, // 0x1010101
        calendarViewShown = 16843596, // 0x101034c
        calendarViewStyle = 16843613, // 0x101035d
        canRetrieveWindowContent = 16843653, // 0x1010385
        candidatesTextStyleSpans = 16843312, // 0x1010230
        capitalize = 16843113, // 0x1010169
        centerBright = 16842956, // 0x10100cc
        centerColor = 16843275, // 0x101020b
        centerDark = 16842952, // 0x10100c8
        centerMedium = 16842959, // 0x10100cf
        centerX = 16843170, // 0x10101a2
        centerY = 16843171, // 0x10101a3
        checkBoxPreferenceStyle = 16842895, // 0x101008f
        checkMark = 16843016, // 0x1010108
        checkable = 16843237, // 0x10101e5
        checkableBehavior = 16843232, // 0x10101e0
        checkboxStyle = 16842860, // 0x101006c
        _checked = 16843014, // 0x1010106
        checkedButton = 16843080, // 0x1010148
        childDivider = 16843025, // 0x1010111
        childIndicator = 16843020, // 0x101010c
        childIndicatorLeft = 16843023, // 0x101010f
        childIndicatorRight = 16843024, // 0x1010110
        choiceMode = 16843051, // 0x101012b
        clearTaskOnLaunch = 16842773, // 0x1010015
        clickable = 16842981, // 0x10100e5
        clipChildren = 16842986, // 0x10100ea
        clipOrientation = 16843274, // 0x101020a
        clipToPadding = 16842987, // 0x10100eb
        codes = 16843330, // 0x1010242
        collapseColumns = 16843083, // 0x101014b
        color = 16843173, // 0x10101a5
        colorActivatedHighlight = 16843664, // 0x1010390
        colorBackground = 16842801, // 0x1010031
        colorBackgroundCacheHint = 16843435, // 0x10102ab
        colorFocusedHighlight = 16843663, // 0x101038f
        colorForeground = 16842800, // 0x1010030
        colorForegroundInverse = 16843270, // 0x1010206
        colorLongPressedHighlight = 16843662, // 0x101038e
        colorMultiSelectHighlight = 16843665, // 0x1010391
        colorPressedHighlight = 16843661, // 0x101038d
        columnCount = 16843639, // 0x1010377
        columnDelay = 16843215, // 0x10101cf
        columnOrderPreserved = 16843640, // 0x1010378
        columnWidth = 16843031, // 0x1010117
        compatibleWidthLimitDp = 16843621, // 0x1010365
        completionHint = 16843122, // 0x1010172
        completionHintView = 16843123, // 0x1010173
        completionThreshold = 16843124, // 0x1010174
        configChanges = 16842783, // 0x101001f
        configure = 16843357, // 0x101025d
        constantSize = 16843158, // 0x1010196
        content = 16843355, // 0x101025b
        contentAuthority = 16843408, // 0x1010290
        contentDescription = 16843379, // 0x1010273
        cropToPadding = 16843043, // 0x1010123
        cursorVisible = 16843090, // 0x1010152
        customNavigationLayout = 16843474, // 0x10102d2
        customTokens = 16843579, // 0x101033b
        cycles = 16843220, // 0x10101d4
        dashGap = 16843175, // 0x10101a7
        dashWidth = 16843174, // 0x10101a6
        data = 16842798, // 0x101002e
        datePickerStyle = 16843612, // 0x101035c
        dateTextAppearance = 16843593, // 0x1010349
        debuggable = 16842767, // 0x101000f
        defaultValue = 16843245, // 0x10101ed
        delay = 16843212, // 0x10101cc
        dependency = 16843244, // 0x10101ec
        descendantFocusability = 16842993, // 0x10100f1
        description = 16842784, // 0x1010020
        detachWallpaper = 16843430, // 0x10102a6
        detailColumn = 16843427, // 0x10102a3
        detailSocialSummary = 16843428, // 0x10102a4
        detailsElementBackground = 16843598, // 0x101034e
        dial = 16843010, // 0x1010102
        dialogIcon = 16843252, // 0x10101f4
        dialogLayout = 16843255, // 0x10101f7
        dialogMessage = 16843251, // 0x10101f3
        dialogPreferenceStyle = 16842897, // 0x1010091
        dialogTheme = 16843528, // 0x1010308
        dialogTitle = 16843250, // 0x10101f2
        digits = 16843110, // 0x1010166
        direction = 16843217, // 0x10101d1
        directionDescriptions = 16843681, // 0x10103a1
        directionPriority = 16843218, // 0x10101d2
        disableDependentsState = 16843249, // 0x10101f1
        disabledAlpha = 16842803, // 0x1010033
        displayOptions = 16843472, // 0x10102d0
        dither = 16843036, // 0x101011c
        divider = 16843049, // 0x1010129
        dividerHeight = 16843050, // 0x101012a
        dividerHorizontal = 16843564, // 0x101032c
        dividerPadding = 16843562, // 0x101032a
        dividerVertical = 16843530, // 0x101030a
        drawSelectorOnTop = 16843004, // 0x10100fc
        drawable = 16843161, // 0x1010199
        drawableBottom = 16843118, // 0x101016e
        drawableEnd = 16843667, // 0x1010393
        drawableLeft = 16843119, // 0x101016f
        drawablePadding = 16843121, // 0x1010171
        drawableRight = 16843120, // 0x1010170
        drawableStart = 16843666, // 0x1010392
        drawableTop = 16843117, // 0x101016d
        drawingCacheQuality = 16842984, // 0x10100e8
        dropDownAnchor = 16843363, // 0x1010263
        dropDownHeight = 16843395, // 0x1010283
        dropDownHintAppearance = 16842888, // 0x1010088
        dropDownHorizontalOffset = 16843436, // 0x10102ac
        dropDownItemStyle = 16842886, // 0x1010086
        dropDownListViewStyle = 16842861, // 0x101006d
        dropDownSelector = 16843125, // 0x1010175
        dropDownSpinnerStyle = 16843478, // 0x10102d6
        dropDownVerticalOffset = 16843437, // 0x10102ad
        dropDownWidth = 16843362, // 0x1010262
        duplicateParentState = 16842985, // 0x10100e9
        duration = 16843160, // 0x1010198
        editTextBackground = 16843602, // 0x1010352
        editTextColor = 16843601, // 0x1010351
        editTextPreferenceStyle = 16842898, // 0x1010092
        editTextStyle = 16842862, // 0x101006e
        editable = 16843115, // 0x101016b
        editorExtras = 16843300, // 0x1010224
        ellipsize = 16842923, // 0x10100ab
        ems = 16843096, // 0x1010158
        enabled = 16842766, // 0x101000e
        endColor = 16843166, // 0x101019e
        endYear = 16843133, // 0x101017d
        enterFadeDuration = 16843532, // 0x101030c
        entries = 16842930, // 0x10100b2
        entryValues = 16843256, // 0x10101f8
        eventsInterceptionEnabled = 16843389, // 0x101027d
        excludeFromRecents = 16842775, // 0x1010017
        exitFadeDuration = 16843533, // 0x101030d
        expandableListPreferredChildIndicatorLeft = 16842834, // 0x1010052
        expandableListPreferredChildIndicatorRight = 16842835, // 0x1010053
        expandableListPreferredChildPaddingLeft = 16842831, // 0x101004f
        expandableListPreferredItemIndicatorLeft = 16842832, // 0x1010050
        expandableListPreferredItemIndicatorRight = 16842833, // 0x1010051
        expandableListPreferredItemPaddingLeft = 16842830, // 0x101004e
        expandableListViewStyle = 16842863, // 0x101006f
        expandableListViewWhiteStyle = 16843446, // 0x10102b6
        exported = 16842768, // 0x1010010
        extraTension = 16843371, // 0x101026b
        factor = 16843219, // 0x10101d3
        fadeDuration = 16843384, // 0x1010278
        fadeEnabled = 16843390, // 0x101027e
        fadeOffset = 16843383, // 0x1010277
        fadeScrollbars = 16843434, // 0x10102aa
        fadingEdge = 16842975, // 0x10100df
        fadingEdgeLength = 16842976, // 0x10100e0
        fastScrollAlwaysVisible = 16843573, // 0x1010335
        fastScrollEnabled = 16843302, // 0x1010226
        fastScrollOverlayPosition = 16843578, // 0x101033a
        fastScrollPreviewBackgroundLeft = 16843575, // 0x1010337
        fastScrollPreviewBackgroundRight = 16843576, // 0x1010338
        fastScrollTextColor = 16843609, // 0x1010359
        fastScrollThumbDrawable = 16843574, // 0x1010336
        fastScrollTrackDrawable = 16843577, // 0x1010339
        fillAfter = 16843197, // 0x10101bd
        fillBefore = 16843196, // 0x10101bc
        fillEnabled = 16843343, // 0x101024f
        fillViewport = 16843130, // 0x101017a
        filter = 16843035, // 0x101011b
        filterTouchesWhenObscured = 16843460, // 0x10102c4
        finishOnCloseSystemDialogs = 16843431, // 0x10102a7
        finishOnTaskLaunch = 16842772, // 0x1010014
        firstDayOfWeek = 16843581, // 0x101033d
        fitsSystemWindows = 16842973, // 0x10100dd
        flipInterval = 16843129, // 0x1010179
        focusable = 16842970, // 0x10100da
        focusableInTouchMode = 16842971, // 0x10100db
        focusedMonthDateColor = 16843587, // 0x1010343
        fontFamily = 16843692, // 0x10103ac
        footerDividersEnabled = 16843311, // 0x101022f
        foreground = 16843017, // 0x1010109
        foregroundGravity = 16843264, // 0x1010200
        format = 16843013, // 0x1010105
        fragment = 16843491, // 0x10102e3
        fragmentCloseEnterAnimation = 16843495, // 0x10102e7
        fragmentCloseExitAnimation = 16843496, // 0x10102e8
        fragmentFadeEnterAnimation = 16843497, // 0x10102e9
        fragmentFadeExitAnimation = 16843498, // 0x10102ea
        fragmentOpenEnterAnimation = 16843493, // 0x10102e5
        fragmentOpenExitAnimation = 16843494, // 0x10102e6
        freezesText = 16843116, // 0x101016c
        fromAlpha = 16843210, // 0x10101ca
        fromDegrees = 16843187, // 0x10101b3
        fromXDelta = 16843206, // 0x10101c6
        fromXScale = 16843202, // 0x10101c2
        fromYDelta = 16843208, // 0x10101c8
        fromYScale = 16843204, // 0x10101c4
        fullBright = 16842954, // 0x10100ca
        fullDark = 16842950, // 0x10100c6
        functionalTest = 16842787, // 0x1010023
        galleryItemBackground = 16842828, // 0x101004c
        galleryStyle = 16842864, // 0x1010070
        gestureColor = 16843381, // 0x1010275
        gestureStrokeAngleThreshold = 16843388, // 0x101027c
        gestureStrokeLengthThreshold = 16843386, // 0x101027a
        gestureStrokeSquarenessThreshold = 16843387, // 0x101027b
        gestureStrokeType = 16843385, // 0x1010279
        gestureStrokeWidth = 16843380, // 0x1010274
        glEsVersion = 16843393, // 0x1010281
        gradientRadius = 16843172, // 0x10101a4
        grantUriPermissions = 16842779, // 0x101001b
        gravity = 16842927, // 0x10100af
        gridViewStyle = 16842865, // 0x1010071
        groupIndicator = 16843019, // 0x101010b
        hand_hour = 16843011, // 0x1010103
        hand_minute = 16843012, // 0x1010104
        handle = 16843354, // 0x101025a
        handleProfiling = 16842786, // 0x1010022
        hapticFeedbackEnabled = 16843358, // 0x101025e
        hardwareAccelerated = 16843475, // 0x10102d3
        hasCode = 16842764, // 0x101000c
        headerBackground = 16843055, // 0x101012f
        headerDividersEnabled = 16843310, // 0x101022e
        height = 16843093, // 0x1010155
        hint = 16843088, // 0x1010150
        homeAsUpIndicator = 16843531, // 0x101030b
        homeLayout = 16843549, // 0x101031d
        horizontalDivider = 16843053, // 0x101012d
        horizontalGap = 16843327, // 0x101023f
        horizontalScrollViewStyle = 16843603, // 0x1010353
        horizontalSpacing = 16843028, // 0x1010114
        host = 16842792, // 0x1010028
        icon = 16842754, // 0x1010002
        iconPreview = 16843337, // 0x1010249
        iconifiedByDefault = 16843514, // 0x10102fa
        id = 16842960, // 0x10100d0
        ignoreGravity = 16843263, // 0x10101ff
        imageButtonStyle = 16842866, // 0x1010072
        imageWellStyle = 16842867, // 0x1010073
        imeActionId = 16843366, // 0x1010266
        imeActionLabel = 16843365, // 0x1010265
        imeExtractEnterAnimation = 16843368, // 0x1010268
        imeExtractExitAnimation = 16843369, // 0x1010269
        imeFullscreenBackground = 16843308, // 0x101022c
        imeOptions = 16843364, // 0x1010264
        imeSubtypeExtraValue = 16843502, // 0x10102ee
        imeSubtypeLocale = 16843500, // 0x10102ec
        imeSubtypeMode = 16843501, // 0x10102ed
        immersive = 16843456, // 0x10102c0
        importantForAccessibility = 16843690, // 0x10103aa
        inAnimation = 16843127, // 0x1010177
        includeFontPadding = 16843103, // 0x101015f
        includeInGlobalSearch = 16843374, // 0x101026e
        indeterminate = 16843065, // 0x1010139
        indeterminateBehavior = 16843070, // 0x101013e
        indeterminateDrawable = 16843067, // 0x101013b
        indeterminateDuration = 16843069, // 0x101013d
        indeterminateOnly = 16843066, // 0x101013a
        indeterminateProgressStyle = 16843544, // 0x1010318
        indicatorLeft = 16843021, // 0x101010d
        indicatorRight = 16843022, // 0x101010e
        inflatedId = 16842995, // 0x10100f3
        initOrder = 16842778, // 0x101001a
        initialLayout = 16843345, // 0x1010251
        innerRadius = 16843359, // 0x101025f
        innerRadiusRatio = 16843163, // 0x101019b
        inputMethod = 16843112, // 0x1010168
        inputType = 16843296, // 0x1010220
        insetBottom = 16843194, // 0x10101ba
        insetLeft = 16843191, // 0x10101b7
        insetRight = 16843192, // 0x10101b8
        insetTop = 16843193, // 0x10101b9
        installLocation = 16843447, // 0x10102b7
        interpolator = 16843073, // 0x1010141
        isAlwaysSyncable = 16843571, // 0x1010333
        isAuxiliary = 16843647, // 0x101037f
        isDefault = 16843297, // 0x1010221
        isIndicator = 16843079, // 0x1010147
        isModifier = 16843334, // 0x1010246
        isRepeatable = 16843336, // 0x1010248
        isScrollContainer = 16843342, // 0x101024e
        isSticky = 16843335, // 0x1010247
        isolatedProcess = 16843689, // 0x10103a9
        itemBackground = 16843056, // 0x1010130
        itemIconDisabledAlpha = 16843057, // 0x1010131
        itemPadding = 16843565, // 0x101032d
        itemTextAppearance = 16843052, // 0x101012c
        keepScreenOn = 16843286, // 0x1010216
        key = 16843240, // 0x10101e8
        keyBackground = 16843315, // 0x1010233
        keyEdgeFlags = 16843333, // 0x1010245
        keyHeight = 16843326, // 0x101023e
        keyIcon = 16843340, // 0x101024c
        keyLabel = 16843339, // 0x101024b
        keyOutputText = 16843338, // 0x101024a
        keyPreviewHeight = 16843321, // 0x1010239
        keyPreviewLayout = 16843319, // 0x1010237
        keyPreviewOffset = 16843320, // 0x1010238
        keyTextColor = 16843318, // 0x1010236
        keyTextSize = 16843316, // 0x1010234
        keyWidth = 16843325, // 0x101023d
        keyboardLayout = 16843691, // 0x10103ab
        keyboardMode = 16843341, // 0x101024d
        keycode = 16842949, // 0x10100c5
        killAfterRestore = 16843420, // 0x101029c
        label = 16842753, // 0x1010001
        labelTextSize = 16843317, // 0x1010235
        largeHeap = 16843610, // 0x101035a
        largeScreens = 16843398, // 0x1010286
        largestWidthLimitDp = 16843622, // 0x1010366
        launchMode = 16842781, // 0x101001d
        layerType = 16843604, // 0x1010354
        layout = 16842994, // 0x10100f2
        layoutAnimation = 16842988, // 0x10100ec
        layoutDirection = 16843698, // 0x10103b2
        layout_above = 16843140, // 0x1010184
        layout_alignBaseline = 16843142, // 0x1010186
        layout_alignBottom = 16843146, // 0x101018a
        layout_alignEnd = 16843706, // 0x10103ba
        layout_alignLeft = 16843143, // 0x1010187
        layout_alignParentBottom = 16843150, // 0x101018e
        layout_alignParentEnd = 16843708, // 0x10103bc
        layout_alignParentLeft = 16843147, // 0x101018b
        layout_alignParentRight = 16843149, // 0x101018d
        layout_alignParentStart = 16843707, // 0x10103bb
        layout_alignParentTop = 16843148, // 0x101018c
        layout_alignRight = 16843145, // 0x1010189
        layout_alignStart = 16843705, // 0x10103b9
        layout_alignTop = 16843144, // 0x1010188
        layout_alignWithParentIfMissing = 16843154, // 0x1010192
        layout_below = 16843141, // 0x1010185
        layout_centerHorizontal = 16843152, // 0x1010190
        layout_centerInParent = 16843151, // 0x101018f
        layout_centerVertical = 16843153, // 0x1010191
        layout_column = 16843084, // 0x101014c
        layout_columnSpan = 16843645, // 0x101037d
        layout_gravity = 16842931, // 0x10100b3
        layout_height = 16842997, // 0x10100f5
        layout_margin = 16842998, // 0x10100f6
        layout_marginBottom = 16843002, // 0x10100fa
        layout_marginEnd = 16843702, // 0x10103b6
        layout_marginLeft = 16842999, // 0x10100f7
        layout_marginRight = 16843001, // 0x10100f9
        layout_marginStart = 16843701, // 0x10103b5
        layout_marginTop = 16843000, // 0x10100f8
        layout_row = 16843643, // 0x101037b
        layout_rowSpan = 16843644, // 0x101037c
        layout_scale = 16843155, // 0x1010193
        layout_span = 16843085, // 0x101014d
        layout_toEndOf = 16843704, // 0x10103b8
        layout_toLeftOf = 16843138, // 0x1010182
        layout_toRightOf = 16843139, // 0x1010183
        layout_toStartOf = 16843703, // 0x10103b7
        layout_weight = 16843137, // 0x1010181
        layout_width = 16842996, // 0x10100f4
        layout_x = 16843135, // 0x101017f
        layout_y = 16843136, // 0x1010180
        left = 16843181, // 0x10101ad
        lineSpacingExtra = 16843287, // 0x1010217
        lineSpacingMultiplier = 16843288, // 0x1010218
        lines = 16843092, // 0x1010154
        linksClickable = 16842929, // 0x10100b1
        listChoiceBackgroundIndicator = 16843504, // 0x10102f0
        listChoiceIndicatorMultiple = 16843290, // 0x101021a
        listChoiceIndicatorSingle = 16843289, // 0x1010219
        listDivider = 16843284, // 0x1010214
        listDividerAlertDialog = 16843525, // 0x1010305
        listPopupWindowStyle = 16843519, // 0x10102ff
        listPreferredItemHeight = 16842829, // 0x101004d
        listPreferredItemHeightLarge = 16843654, // 0x1010386
        listPreferredItemHeightSmall = 16843655, // 0x1010387
        listPreferredItemPaddingEnd = 16843710, // 0x10103be
        listPreferredItemPaddingLeft = 16843683, // 0x10103a3
        listPreferredItemPaddingRight = 16843684, // 0x10103a4
        listPreferredItemPaddingStart = 16843709, // 0x10103bd
        listSelector = 16843003, // 0x10100fb
        listSeparatorTextViewStyle = 16843272, // 0x1010208
        listViewStyle = 16842868, // 0x1010074
        listViewWhiteStyle = 16842869, // 0x1010075
        logo = 16843454, // 0x10102be
        longClickable = 16842982, // 0x10100e6
        loopViews = 16843527, // 0x1010307
        manageSpaceActivity = 16842756, // 0x1010004
        mapViewStyle = 16842890, // 0x101008a
        marqueeRepeatLimit = 16843293, // 0x101021d
        max = 16843062, // 0x1010136
        maxDate = 16843584, // 0x1010340
        maxEms = 16843095, // 0x1010157
        maxHeight = 16843040, // 0x1010120
        maxItemsPerRow = 16843060, // 0x1010134
        maxLength = 16843104, // 0x1010160
        maxLevel = 16843186, // 0x10101b2
        maxLines = 16843091, // 0x1010153
        maxRows = 16843059, // 0x1010133
        maxSdkVersion = 16843377, // 0x1010271
        maxWidth = 16843039, // 0x101011f
        measureAllChildren = 16843018, // 0x101010a
        measureWithLargestChild = 16843476, // 0x10102d4
        mediaRouteButtonStyle = 16843693, // 0x10103ad
        mediaRouteTypes = 16843694, // 0x10103ae
        menuCategory = 16843230, // 0x10101de
        mimeType = 16842790, // 0x1010026
        minDate = 16843583, // 0x101033f
        minEms = 16843098, // 0x101015a
        minHeight = 16843072, // 0x1010140
        minLevel = 16843185, // 0x10101b1
        minLines = 16843094, // 0x1010156
        minResizeHeight = 16843670, // 0x1010396
        minResizeWidth = 16843669, // 0x1010395
        minSdkVersion = 16843276, // 0x101020c
        minWidth = 16843071, // 0x101013f
        mode = 16843134, // 0x101017e
        moreIcon = 16843061, // 0x1010135
        multiprocess = 16842771, // 0x1010013
        name = 16842755, // 0x1010003
        navigationMode = 16843471, // 0x10102cf
        negativeButtonText = 16843254, // 0x10101f6
        nextFocusDown = 16842980, // 0x10100e4
        nextFocusForward = 16843580, // 0x101033c
        nextFocusLeft = 16842977, // 0x10100e1
        nextFocusRight = 16842978, // 0x10100e2
        nextFocusUp = 16842979, // 0x10100e3
        noHistory = 16843309, // 0x101022d
        normalScreens = 16843397, // 0x1010285
        notificationTimeout = 16843651, // 0x1010383
        numColumns = 16843032, // 0x1010118
        numStars = 16843076, // 0x1010144
        numeric = 16843109, // 0x1010165
        numericShortcut = 16843236, // 0x10101e4
        onClick = 16843375, // 0x101026f
        oneshot = 16843159, // 0x1010197
        opacity = 16843550, // 0x101031e
        order = 16843242, // 0x10101ea
        orderInCategory = 16843231, // 0x10101df
        ordering = 16843490, // 0x10102e2
        orderingFromXml = 16843239, // 0x10101e7
        orientation = 16842948, // 0x10100c4
        outAnimation = 16843128, // 0x1010178
        overScrollFooter = 16843459, // 0x10102c3
        overScrollHeader = 16843458, // 0x10102c2
        overScrollMode = 16843457, // 0x10102c1
        overridesImplicitlyEnabledSubtype = 16843682, // 0x10103a2
        packageNames = 16843649, // 0x1010381
        padding = 16842965, // 0x10100d5
        paddingBottom = 16842969, // 0x10100d9
        paddingEnd = 16843700, // 0x10103b4
        paddingLeft = 16842966, // 0x10100d6
        paddingRight = 16842968, // 0x10100d8
        paddingStart = 16843699, // 0x10103b3
        paddingTop = 16842967, // 0x10100d7
        panelBackground = 16842846, // 0x101005e
        panelColorBackground = 16842849, // 0x1010061
        panelColorForeground = 16842848, // 0x1010060
        panelFullBackground = 16842847, // 0x101005f
        panelTextAppearance = 16842850, // 0x1010062
        parentActivityName = 16843687, // 0x10103a7
        password = 16843100, // 0x101015c
        path = 16842794, // 0x101002a
        pathPattern = 16842796, // 0x101002c
        pathPrefix = 16842795, // 0x101002b
        permission = 16842758, // 0x1010006
        permissionGroup = 16842762, // 0x101000a
        persistent = 16842765, // 0x101000d
        persistentDrawingCache = 16842990, // 0x10100ee
        phoneNumber = 16843111, // 0x1010167
        pivotX = 16843189, // 0x10101b5
        pivotY = 16843190, // 0x10101b6
        popupAnimationStyle = 16843465, // 0x10102c9
        popupBackground = 16843126, // 0x1010176
        popupCharacters = 16843332, // 0x1010244
        popupKeyboard = 16843331, // 0x1010243
        popupLayout = 16843323, // 0x101023b
        popupMenuStyle = 16843520, // 0x1010300
        popupWindowStyle = 16842870, // 0x1010076
        port = 16842793, // 0x1010029
        positiveButtonText = 16843253, // 0x10101f5
        preferenceCategoryStyle = 16842892, // 0x101008c
        preferenceInformationStyle = 16842893, // 0x101008d
        preferenceLayoutChild = 16842900, // 0x1010094
        preferenceScreenStyle = 16842891, // 0x101008b
        preferenceStyle = 16842894, // 0x101008e
        previewImage = 16843482, // 0x10102da
        priority = 16842780, // 0x101001c
        privateImeOptions = 16843299, // 0x1010223
        process = 16842769, // 0x1010011
        progress = 16843063, // 0x1010137
        progressBarPadding = 16843545, // 0x1010319
        progressBarStyle = 16842871, // 0x1010077
        progressBarStyleHorizontal = 16842872, // 0x1010078
        progressBarStyleInverse = 16843399, // 0x1010287
        progressBarStyleLarge = 16842874, // 0x101007a
        progressBarStyleLargeInverse = 16843401, // 0x1010289
        progressBarStyleSmall = 16842873, // 0x1010079
        progressBarStyleSmallInverse = 16843400, // 0x1010288
        progressBarStyleSmallTitle = 16843279, // 0x101020f
        progressDrawable = 16843068, // 0x101013c
        prompt = 16843131, // 0x101017b
        propertyName = 16843489, // 0x10102e1
        protectionLevel = 16842761, // 0x1010009
        publicKey = 16843686, // 0x10103a6
        queryActionMsg = 16843227, // 0x10101db
        queryAfterZeroResults = 16843394, // 0x1010282
        queryHint = 16843608, // 0x1010358
        quickContactBadgeStyleSmallWindowLarge = 16843443, // 0x10102b3
        quickContactBadgeStyleSmallWindowMedium = 16843442, // 0x10102b2
        quickContactBadgeStyleSmallWindowSmall = 16843441, // 0x10102b1
        quickContactBadgeStyleWindowLarge = 16843440, // 0x10102b0
        quickContactBadgeStyleWindowMedium = 16843439, // 0x10102af
        quickContactBadgeStyleWindowSmall = 16843438, // 0x10102ae
        radioButtonStyle = 16842878, // 0x101007e
        radius = 16843176, // 0x10101a8
        rating = 16843077, // 0x1010145
        ratingBarStyle = 16842876, // 0x101007c
        ratingBarStyleIndicator = 16843280, // 0x1010210
        ratingBarStyleSmall = 16842877, // 0x101007d
        readPermission = 16842759, // 0x1010007
        repeatCount = 16843199, // 0x10101bf
        repeatMode = 16843200, // 0x10101c0
        reqFiveWayNav = 16843314, // 0x1010232
        reqHardKeyboard = 16843305, // 0x1010229
        reqKeyboardType = 16843304, // 0x1010228
        reqNavigation = 16843306, // 0x101022a
        reqTouchScreen = 16843303, // 0x1010227
        required = 16843406, // 0x101028e
        requiresFadingEdge = 16843685, // 0x10103a5
        requiresSmallestWidthDp = 16843620, // 0x1010364
        resizeMode = 16843619, // 0x1010363
        resizeable = 16843405, // 0x101028d
        resource = 16842789, // 0x1010025
        restoreAnyVersion = 16843450, // 0x10102ba
        restoreNeedsApplication = 16843421, // 0x101029d
        right = 16843183, // 0x10101af
        ringtonePreferenceStyle = 16842899, // 0x1010093
        ringtoneType = 16843257, // 0x10101f9
        rotation = 16843558, // 0x1010326
        rotationX = 16843559, // 0x1010327
        rotationY = 16843560, // 0x1010328
        rowCount = 16843637, // 0x1010375
        rowDelay = 16843216, // 0x10101d0
        rowEdgeFlags = 16843329, // 0x1010241
        rowHeight = 16843058, // 0x1010132
        rowOrderPreserved = 16843638, // 0x1010376
        saveEnabled = 16842983, // 0x10100e7
        scaleGravity = 16843262, // 0x10101fe
        scaleHeight = 16843261, // 0x10101fd
        scaleType = 16843037, // 0x101011d
        scaleWidth = 16843260, // 0x10101fc
        scaleX = 16843556, // 0x1010324
        scaleY = 16843557, // 0x1010325
        scheme = 16842791, // 0x1010027
        screenDensity = 16843467, // 0x10102cb
        screenOrientation = 16842782, // 0x101001e
        screenSize = 16843466, // 0x10102ca
        scrollHorizontally = 16843099, // 0x101015b
        scrollViewStyle = 16842880, // 0x1010080
        scrollX = 16842962, // 0x10100d2
        scrollY = 16842963, // 0x10100d3
        scrollbarAlwaysDrawHorizontalTrack = 16842856, // 0x1010068
        scrollbarAlwaysDrawVerticalTrack = 16842857, // 0x1010069
        scrollbarDefaultDelayBeforeFade = 16843433, // 0x10102a9
        scrollbarFadeDuration = 16843432, // 0x10102a8
        scrollbarSize = 16842851, // 0x1010063
        scrollbarStyle = 16842879, // 0x101007f
        scrollbarThumbHorizontal = 16842852, // 0x1010064
        scrollbarThumbVertical = 16842853, // 0x1010065
        scrollbarTrackHorizontal = 16842854, // 0x1010066
        scrollbarTrackVertical = 16842855, // 0x1010067
        scrollbars = 16842974, // 0x10100de
        scrollingCache = 16843006, // 0x10100fe
        searchButtonText = 16843269, // 0x1010205
        searchMode = 16843221, // 0x10101d5
        searchSettingsDescription = 16843402, // 0x101028a
        searchSuggestAuthority = 16843222, // 0x10101d6
        searchSuggestIntentAction = 16843225, // 0x10101d9
        searchSuggestIntentData = 16843226, // 0x10101da
        searchSuggestPath = 16843223, // 0x10101d7
        searchSuggestSelection = 16843224, // 0x10101d8
        searchSuggestThreshold = 16843373, // 0x101026d
        secondaryProgress = 16843064, // 0x1010138
        seekBarStyle = 16842875, // 0x101007b
        segmentedButtonStyle = 16843568, // 0x1010330
        selectAllOnFocus = 16843102, // 0x101015e
        selectable = 16843238, // 0x10101e6
        selectableItemBackground = 16843534, // 0x101030e
        selectedDateVerticalBar = 16843591, // 0x1010347
        selectedWeekBackgroundColor = 16843586, // 0x1010342
        settingsActivity = 16843301, // 0x1010225
        shadowColor = 16843105, // 0x1010161
        shadowDx = 16843106, // 0x1010162
        shadowDy = 16843107, // 0x1010163
        shadowRadius = 16843108, // 0x1010164
        shape = 16843162, // 0x101019a
        shareInterpolator = 16843195, // 0x10101bb
        sharedUserId = 16842763, // 0x101000b
        sharedUserLabel = 16843361, // 0x1010261
        shouldDisableView = 16843246, // 0x10101ee
        showAsAction = 16843481, // 0x10102d9
        showDefault = 16843258, // 0x10101fa
        showDividers = 16843561, // 0x1010329
        showSilent = 16843259, // 0x10101fb
        showWeekNumber = 16843582, // 0x101033e
        shownWeekCount = 16843585, // 0x1010341
        shrinkColumns = 16843082, // 0x101014a
        singleLine = 16843101, // 0x101015d
        smallIcon = 16843422, // 0x101029e
        smallScreens = 16843396, // 0x1010284
        smoothScrollbar = 16843313, // 0x1010231
        soundEffectsEnabled = 16843285, // 0x1010215
        spacing = 16843027, // 0x1010113
        spinnerDropDownItemStyle = 16842887, // 0x1010087
        spinnerItemStyle = 16842889, // 0x1010089
        spinnerMode = 16843505, // 0x10102f1
        spinnerStyle = 16842881, // 0x1010081
        spinnersShown = 16843595, // 0x101034b
        splitMotionEvents = 16843503, // 0x10102ef
        src = 16843033, // 0x1010119
        stackFromBottom = 16843005, // 0x10100fd
        starStyle = 16842882, // 0x1010082
        startColor = 16843165, // 0x101019d
        startOffset = 16843198, // 0x10101be
        startYear = 16843132, // 0x101017c
        stateNotNeeded = 16842774, // 0x1010016
        state_above_anchor = 16842922, // 0x10100aa
        state_accelerated = 16843547, // 0x101031b
        state_activated = 16843518, // 0x10102fe
        state_active = 16842914, // 0x10100a2
        state_checkable = 16842911, // 0x101009f
        state_checked = 16842912, // 0x10100a0
        state_drag_can_accept = 16843624, // 0x1010368
        state_drag_hovered = 16843625, // 0x1010369
        state_empty = 16842921, // 0x10100a9
        state_enabled = 16842910, // 0x101009e
        state_expanded = 16842920, // 0x10100a8
        state_first = 16842916, // 0x10100a4
        state_focused = 16842908, // 0x101009c
        state_hovered = 16843623, // 0x1010367
        state_last = 16842918, // 0x10100a6
        state_long_pressable = 16843324, // 0x101023c
        state_middle = 16842917, // 0x10100a5
        state_multiline = 16843597, // 0x101034d
        state_pressed = 16842919, // 0x10100a7
        state_selected = 16842913, // 0x10100a1
        state_single = 16842915, // 0x10100a3
        state_window_focused = 16842909, // 0x101009d
        staticWallpaperPreview = 16843569, // 0x1010331
        stepSize = 16843078, // 0x1010146
        stopWithTask = 16843626, // 0x101036a
        streamType = 16843273, // 0x1010209
        stretchColumns = 16843081, // 0x1010149
        stretchMode = 16843030, // 0x1010116
        subtitle = 16843473, // 0x10102d1
        subtitleTextStyle = 16843513, // 0x10102f9
        subtypeExtraValue = 16843674, // 0x101039a
        subtypeLocale = 16843673, // 0x1010399
        suggestActionMsg = 16843228, // 0x10101dc
        suggestActionMsgColumn = 16843229, // 0x10101dd
        summary = 16843241, // 0x10101e9
        summaryColumn = 16843426, // 0x10102a2
        summaryOff = 16843248, // 0x10101f0
        summaryOn = 16843247, // 0x10101ef
        supportsRtl = 16843695, // 0x10103af
        supportsUploading = 16843419, // 0x101029b
        switchMinWidth = 16843632, // 0x1010370
        switchPadding = 16843633, // 0x1010371
        switchPreferenceStyle = 16843629, // 0x101036d
        switchTextAppearance = 16843630, // 0x101036e
        switchTextOff = 16843628, // 0x101036c
        switchTextOn = 16843627, // 0x101036b
        syncable = 16842777, // 0x1010019
        tabStripEnabled = 16843453, // 0x10102bd
        tabStripLeft = 16843451, // 0x10102bb
        tabStripRight = 16843452, // 0x10102bc
        tabWidgetStyle = 16842883, // 0x1010083
        tag = 16842961, // 0x10100d1
        targetActivity = 16843266, // 0x1010202
        targetClass = 16842799, // 0x101002f
        targetDescriptions = 16843680, // 0x10103a0
        targetPackage = 16842785, // 0x1010021
        targetSdkVersion = 16843376, // 0x1010270
        taskAffinity = 16842770, // 0x1010012
        taskCloseEnterAnimation = 16842942, // 0x10100be
        taskCloseExitAnimation = 16842943, // 0x10100bf
        taskOpenEnterAnimation = 16842940, // 0x10100bc
        taskOpenExitAnimation = 16842941, // 0x10100bd
        taskToBackEnterAnimation = 16842946, // 0x10100c2
        taskToBackExitAnimation = 16842947, // 0x10100c3
        taskToFrontEnterAnimation = 16842944, // 0x10100c0
        taskToFrontExitAnimation = 16842945, // 0x10100c1
        tension = 16843370, // 0x101026a
        testOnly = 16843378, // 0x1010272
        text = 16843087, // 0x101014f
        textAlignment = 16843697, // 0x10103b1
        textAllCaps = 16843660, // 0x101038c
        textAppearance = 16842804, // 0x1010034
        textAppearanceButton = 16843271, // 0x1010207
        textAppearanceInverse = 16842805, // 0x1010035
        textAppearanceLarge = 16842816, // 0x1010040
        textAppearanceLargeInverse = 16842819, // 0x1010043
        textAppearanceLargePopupMenu = 16843521, // 0x1010301
        textAppearanceListItem = 16843678, // 0x101039e
        textAppearanceListItemSmall = 16843679, // 0x101039f
        textAppearanceMedium = 16842817, // 0x1010041
        textAppearanceMediumInverse = 16842820, // 0x1010044
        textAppearanceSearchResultSubtitle = 16843424, // 0x10102a0
        textAppearanceSearchResultTitle = 16843425, // 0x10102a1
        textAppearanceSmall = 16842818, // 0x1010042
        textAppearanceSmallInverse = 16842821, // 0x1010045
        textAppearanceSmallPopupMenu = 16843522, // 0x1010302
        textCheckMark = 16842822, // 0x1010046
        textCheckMarkInverse = 16842823, // 0x1010047
        textColor = 16842904, // 0x1010098
        textColorAlertDialogListItem = 16843526, // 0x1010306
        textColorHighlight = 16842905, // 0x1010099
        textColorHighlightInverse = 16843599, // 0x101034f
        textColorHint = 16842906, // 0x101009a
        textColorHintInverse = 16842815, // 0x101003f
        textColorLink = 16842907, // 0x101009b
        textColorLinkInverse = 16843600, // 0x1010350
        textColorPrimary = 16842806, // 0x1010036
        textColorPrimaryDisableOnly = 16842807, // 0x1010037
        textColorPrimaryInverse = 16842809, // 0x1010039
        textColorPrimaryInverseDisableOnly = 16843403, // 0x101028b
        textColorPrimaryInverseNoDisable = 16842813, // 0x101003d
        textColorPrimaryNoDisable = 16842811, // 0x101003b
        textColorSecondary = 16842808, // 0x1010038
        textColorSecondaryInverse = 16842810, // 0x101003a
        textColorSecondaryInverseNoDisable = 16842814, // 0x101003e
        textColorSecondaryNoDisable = 16842812, // 0x101003c
        textColorTertiary = 16843282, // 0x1010212
        textColorTertiaryInverse = 16843283, // 0x1010213
        textCursorDrawable = 16843618, // 0x1010362
        textDirection = 16843696, // 0x10103b0
        textEditNoPasteWindowLayout = 16843541, // 0x1010315
        textEditPasteWindowLayout = 16843540, // 0x1010314
        textEditSideNoPasteWindowLayout = 16843615, // 0x101035f
        textEditSidePasteWindowLayout = 16843614, // 0x101035e
        textEditSuggestionItemLayout = 16843636, // 0x1010374
        textFilterEnabled = 16843007, // 0x10100ff
        textIsSelectable = 16843542, // 0x1010316
        textOff = 16843045, // 0x1010125
        textOn = 16843044, // 0x1010124
        textScaleX = 16843089, // 0x1010151
        textSelectHandle = 16843463, // 0x10102c7
        textSelectHandleLeft = 16843461, // 0x10102c5
        textSelectHandleRight = 16843462, // 0x10102c6
        textSelectHandleWindowStyle = 16843464, // 0x10102c8
        textSize = 16842901, // 0x1010095
        textStyle = 16842903, // 0x1010097
        textSuggestionsWindowStyle = 16843635, // 0x1010373
        textViewStyle = 16842884, // 0x1010084
        theme = 16842752, // 0x1010000
        thickness = 16843360, // 0x1010260
        thicknessRatio = 16843164, // 0x101019c
        thumb = 16843074, // 0x1010142
        thumbOffset = 16843075, // 0x1010143
        thumbTextPadding = 16843634, // 0x1010372
        thumbnail = 16843429, // 0x10102a5
        tileMode = 16843265, // 0x1010201
        tint = 16843041, // 0x1010121
        title = 16843233, // 0x10101e1
        titleCondensed = 16843234, // 0x10101e2
        titleTextStyle = 16843512, // 0x10102f8
        toAlpha = 16843211, // 0x10101cb
        toDegrees = 16843188, // 0x10101b4
        toXDelta = 16843207, // 0x10101c7
        toXScale = 16843203, // 0x10101c3
        toYDelta = 16843209, // 0x10101c9
        toYScale = 16843205, // 0x10101c5
        top = 16843182, // 0x10101ae
        topBright = 16842955, // 0x10100cb
        topDark = 16842951, // 0x10100c7
        topLeftRadius = 16843177, // 0x10101a9
        topOffset = 16843352, // 0x1010258
        topRightRadius = 16843178, // 0x10101aa
        track = 16843631, // 0x101036f
        transcriptMode = 16843008, // 0x1010100
        transformPivotX = 16843552, // 0x1010320
        transformPivotY = 16843553, // 0x1010321
        translationX = 16843554, // 0x1010322
        translationY = 16843555, // 0x1010323
        type = 16843169, // 0x10101a1
        typeface = 16842902, // 0x1010096
        uiOptions = 16843672, // 0x1010398
        uncertainGestureColor = 16843382, // 0x1010276
        unfocusedMonthDateColor = 16843588, // 0x1010344
        unselectedAlpha = 16843278, // 0x101020e
        updatePeriodMillis = 16843344, // 0x1010250
        useDefaultMargins = 16843641, // 0x1010379
        useIntrinsicSizeAsMinimum = 16843536, // 0x1010310
        useLevel = 16843167, // 0x101019f
        userVisible = 16843409, // 0x1010291
        value = 16842788, // 0x1010024
        valueFrom = 16843486, // 0x10102de
        valueTo = 16843487, // 0x10102df
        valueType = 16843488, // 0x10102e0
        variablePadding = 16843157, // 0x1010195
        versionCode = 16843291, // 0x101021b
        versionName = 16843292, // 0x101021c
        verticalCorrection = 16843322, // 0x101023a
        verticalDivider = 16843054, // 0x101012e
        verticalGap = 16843328, // 0x1010240
        verticalScrollbarPosition = 16843572, // 0x1010334
        verticalSpacing = 16843029, // 0x1010115
        visibility = 16842972, // 0x10100dc
        visible = 16843156, // 0x1010194
        vmSafeMode = 16843448, // 0x10102b8
        voiceLanguage = 16843349, // 0x1010255
        voiceLanguageModel = 16843347, // 0x1010253
        voiceMaxResults = 16843350, // 0x1010256
        voicePromptText = 16843348, // 0x1010254
        voiceSearchMode = 16843346, // 0x1010252
        wallpaperCloseEnterAnimation = 16843413, // 0x1010295
        wallpaperCloseExitAnimation = 16843414, // 0x1010296
        wallpaperIntraCloseEnterAnimation = 16843417, // 0x1010299
        wallpaperIntraCloseExitAnimation = 16843418, // 0x101029a
        wallpaperIntraOpenEnterAnimation = 16843415, // 0x1010297
        wallpaperIntraOpenExitAnimation = 16843416, // 0x1010298
        wallpaperOpenEnterAnimation = 16843411, // 0x1010293
        wallpaperOpenExitAnimation = 16843412, // 0x1010294
        webTextViewStyle = 16843449, // 0x10102b9
        webViewStyle = 16842885, // 0x1010085
        weekDayTextAppearance = 16843592, // 0x1010348
        weekNumberColor = 16843589, // 0x1010345
        weekSeparatorLineColor = 16843590, // 0x1010346
        weightSum = 16843048, // 0x1010128
        widgetLayout = 16843243, // 0x10101eb
        width = 16843097, // 0x1010159
        windowActionBar = 16843469, // 0x10102cd
        windowActionBarOverlay = 16843492, // 0x10102e4
        windowActionModeOverlay = 16843485, // 0x10102dd
        windowAnimationStyle = 16842926, // 0x10100ae
        windowBackground = 16842836, // 0x1010054
        windowCloseOnTouchOutside = 16843611, // 0x101035b
        windowContentOverlay = 16842841, // 0x1010059
        windowDisablePreview = 16843298, // 0x1010222
        windowEnableSplitTouch = 16843543, // 0x1010317
        windowEnterAnimation = 16842932, // 0x10100b4
        windowExitAnimation = 16842933, // 0x10100b5
        windowFrame = 16842837, // 0x1010055
        windowFullscreen = 16843277, // 0x101020d
        windowHideAnimation = 16842935, // 0x10100b7
        windowIsFloating = 16842839, // 0x1010057
        windowIsTranslucent = 16842840, // 0x1010058
        windowMinWidthMajor = 16843606, // 0x1010356
        windowMinWidthMinor = 16843607, // 0x1010357
        windowNoDisplay = 16843294, // 0x101021e
        windowNoTitle = 16842838, // 0x1010056
        windowShowAnimation = 16842934, // 0x10100b6
        windowShowWallpaper = 16843410, // 0x1010292
        windowSoftInputMode = 16843307, // 0x101022b
        windowTitleBackgroundStyle = 16842844, // 0x101005c
        windowTitleSize = 16842842, // 0x101005a
        windowTitleStyle = 16842843, // 0x101005b
        writePermission = 16842760, // 0x1010008
        x = 16842924, // 0x10100ac
        xlargeScreens = 16843455, // 0x10102bf
        y = 16842925, // 0x10100ad
        yesNoPreferenceStyle = 16842896, // 0x1010090
        zAdjustment = 16843201, // 0x10101c1
    }

    enum Density : uint
    {
        Density120 = 36,
        Density160 = 48,
        Density240 = 72,
        Density320 = 96,
        Density480 = 144,
        Density640 = 192
    }

    public enum ResourceConfig : int
    {
        MCC = 0,
        MNC = 1,
        Locale = 2,
        TouchScreen = 3,
        Keyboard = 4,
        Density = 10
    }

    enum gradientType : int
    {
        linear = 0,
        radial = 1,
        sweep = 2,
        unsupported
    }

    enum shapeType : int
    {
        rectangle = 0,
        oval = 1,
        line = 2,
        ring = 3,
        unsupported         
    }

    public class ApkReader : AppPackageReader
    {
        private ZipFile zip;
        private byte[] resources;
        private byte[] manifest;

        // ---- 诊断信息（供 IconPath/IconSourceSize/IconTrace 暴露）----
        private string iconPath = "";
        private Size iconSourceSize = Size.Empty;
        private StringBuilder iconTrace = new StringBuilder();

        // convertData 最近一次解析出的 TYPE_REFERENCE 资源（含全部 config）
        private ApkResource lastReferenceResource = null;
        private Dictionary<string, int> ordinalIndex = null;
        private Dictionary<string, int> ordinalIgnoreCaseIndex = null;

        private const string AndroidManifestXML = @"AndroidManifest.xml";
        private const string Resources_arsc = @"resources.arsc";
        private const string TagApplication = @"manifest/application";
        private const string TagManifest = @"manifest";
        private const string AttrLabel = @"label";
        private const string AttrVersionName = @"versionName";
        private const string AttrVersionCode = @"versionCode";
        private const string AttrIcon = @"icon";
        private const string AttrPackage = @"package";
        //private const string AttrName = @"name";
        private const string AttrDebuggable = @"debuggable";
        private const string ConstTrue = "true";
        private const string ConstFalse = "false";
        private const string viewportWidth = "viewportWidth";
        private const string viewportHeight = "viewportHeight";
        private const string adaptive_icon = @"adaptive-icon";

        /// <summary>
        /// extract the manifext
        /// </summary>
        /// <param name="filename">full path to the file</param>
        /// <param name="culture"></param>
        public ApkReader(string filename, string culture = "") {
            FileName = filename;
            openStream(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public ApkReader(Stream stream, string culture = "") {
            Log("Opening apk from stream");
            openStream(stream);
        }

        private void openStream(Stream stream) {
            zip = new ZipFile(stream);

            // FIX: 拆分 APK（ABI/language split）里没有 AndroidManifest.xml 或 resources.arsc，
            // 旧实现直接对 null 求 GetInputStream，抛 ArgumentNullException 一路冒到 Shell。
            // 这里做存在性校验，缺失时置空，让上层优雅回退到默认类型图标。
            ZipEntry en = GetEntryOrdinal(AndroidManifestXML);
            if (en != null) {
                BinaryReader s = new BinaryReader(zip.GetInputStream(en));
                manifest = s.ReadBytes((int)en.Size);
            } else {
                Log("AndroidManifest.xml is missing (split apk / broken archive)");
                manifest = null;
            }

            en = GetEntryOrdinal(Resources_arsc);
            if (en != null) {
                BinaryReader s = new BinaryReader(zip.GetInputStream(en));
                resources = s.ReadBytes((int)en.Size);
            } else {
                Log("resources.arsc is missing (split apk / broken archive)");
                resources = null;
            }
        }

        public override string IconPath {
            get { return iconPath; }
        }

        public override Size IconSourceSize {
            get { return iconSourceSize; }
        }

        public override string IconTrace {
            get { return iconTrace.ToString(); }
        }

        /// <summary>
        /// 条目查找（必须使用 ordinal 比较）。
        ///
        /// SharpZipLib 的 ZipFile.FindEntry(name, ignoreCase:true) 与内部 _index 使用
        /// InvariantCulture 忽略大小写比较。对于 MT管理器 / NP管理器 / UNMgr 这类把资源名
        /// 整体替换成阿拉伯-叙利亚文「组合符号」的混淆 APK，这种比较会把大量**不同**的条目
        /// 判为相等：实测 MT Manager 2.9.7 中 "ۦۧۘ" 有 11 个条目的 Compare() 为 0，
        /// 查找于是命中索引最小的邻居（#220 一个 layout XML），而真正的图标 PNG 在 #981。
        /// 后果不只是「取不到图标」，还可能**静默显示成完全无关的图案**。
        /// </summary>
        private int FindEntryOrdinal(string name) {
            if (name == null) return -1;
            if (ordinalIndex == null) {
                ordinalIndex = new Dictionary<string, int>(StringComparer.Ordinal);
                ordinalIgnoreCaseIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < zip.Count; i++) {
                    ZipEntry e = zip[i];
                    if (e == null || e.Name == null) continue;
                    if (!ordinalIndex.ContainsKey(e.Name)) ordinalIndex[e.Name] = i;
                    if (!ordinalIgnoreCaseIndex.ContainsKey(e.Name)) ordinalIgnoreCaseIndex[e.Name] = i;
                }
            }
            int idx;
            if (ordinalIndex.TryGetValue(name, out idx)) return idx;
            // 兜底：ordinal 忽略大小写（注意不是 culture-aware，避免上面描述的误判）
            if (ordinalIgnoreCaseIndex.TryGetValue(name, out idx)) return idx;
            return -1;
        }

        private ZipEntry GetEntryOrdinal(string name) {
            int i = FindEntryOrdinal(name);
            return (i >= 0) ? zip[i] : null;
        }

        /// <summary>诊断：打印 SharpZipLib 对某个条目名的解析结果（索引/名字/大小/前 16 字节）。</summary>
        public string ZipTrace(string path) {
            StringBuilder sb = new StringBuilder();
            try {
                int idxFind = zip.FindEntry(path, true);
                ZipEntry ci = zip.GetEntry(path);
                sb.Append("FindEntry(ci)=").Append(idxFind)
                  .Append("; GetEntry=").Append(ci == null ? "null" : ("name=" + ci.Name + " size=" + ci.Size + " idx=" + ci.ZipFileIndex));
                sb.Append("; totalEntries=").Append(zip.Count);
                if (ci != null) {
                    byte[] head = ReadEntryHead(ci, 16);
                    sb.Append("; head=").Append(head == null ? "null" : BitConverter.ToString(head));
                    sb.Append("; sniff=").Append(SniffImageKind(head) ?? "(none)");
                }
                // 线性扫描所有条目，找出被 SharpZipLib 认为是同一个名字的条目
                sb.Append("\n  linear scan (Compare(ignoreCase,Invariant)):");
                for (int i = 0; i < zip.Count; i++) {
                    ZipEntry e = zip[i];
                    if (e == null) continue;
                    if (string.Equals(e.Name, path, StringComparison.Ordinal) ||
                        string.Compare(path, e.Name, true, CultureInfo.InvariantCulture) == 0) {
                        sb.Append("\n   #").Append(i).Append(" ")
                          .Append(string.Equals(e.Name, path, StringComparison.Ordinal) ? "ORDINAL-EQ" : "CULTURE-EQ-ONLY")
                          .Append(" name=").Append(e.Name).Append(" size=").Append(e.Size)
                          .Append(" head=").Append(BitConverter.ToString(ReadEntryHead(e, 8) ?? new byte[0]));
                    }
                }
            } catch (Exception ex) {
                sb.Append("EX ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 诊断：追踪某个资源 ID 在 resources.arsc 中的查找过程（定位引用解析失败原因）。
        /// </summary>
        public string ArscTrace(uint id) {
            StringBuilder sb = new StringBuilder();
            if (resources == null) { return "resources.arsc is null"; }
            sb.Append("resources.Length=").Append(resources.Length).Append("; ");
            uint pkgId = (id & 0xff000000) >> 24, typeId = (id & 0x00ff0000) >> 16, entryId = id & 0xffff;
            sb.Append("id=0x").Append(id.ToString("X8"))
              .Append(" pkg=0x").Append(pkgId.ToString("X2"))
              .Append(" type=").Append(typeId).Append(" entry=").Append(entryId).Append("; ");
            using (MemoryStream ms = new MemoryStream(resources))
            using (BinaryReader br = new BinaryReader(ms)) {
                RES_TYPE rootType = (RES_TYPE)br.ReadInt16();
                short rootHsize = br.ReadInt16();
                int rootSize = br.ReadInt32();
                int packageCount = br.ReadInt32();
                sb.Append("rootType=0x").Append(((int)rootType).ToString("X4"))
                  .Append(" hsize=").Append(rootHsize).Append(" size=").Append(rootSize)
                  .Append(" pkgCount=").Append(packageCount).Append("; ");
                ms.Seek(rootHsize, SeekOrigin.Begin);
                ms.Seek(4, SeekOrigin.Current);
                int spSize = br.ReadInt32();
                sb.Append("globalStringPoolSize=").Append(spSize).Append("; ");
                ms.Seek(spSize - 8, SeekOrigin.Current);
                for (int pack = 0; pack < packageCount; pack++) {
                    long PackChunkPos = ms.Position;
                    ms.Seek(2, SeekOrigin.Current);
                    int headerSize = br.ReadInt16();
                    int PackChunkSize = br.ReadInt32();
                    int packID = br.ReadInt32();
                    sb.Append("\n pkg#").Append(pack).Append(" id=0x").Append(packID.ToString("X2"))
                      .Append(" hsize=").Append(headerSize).Append(" size=").Append(PackChunkSize);
                    if (packID != pkgId) { ms.Seek(PackChunkPos + PackChunkSize, SeekOrigin.Begin); continue; }
                    ms.Seek(PackChunkPos + headerSize, SeekOrigin.Begin);
                    ms.Seek(4, SeekOrigin.Current);
                    int tsSize = br.ReadInt32();
                    sb.Append(" typeStrPoolSize=").Append(tsSize);
                    ms.Seek(tsSize - 8, SeekOrigin.Current);
                    ms.Seek(4, SeekOrigin.Current);
                    int ksSize = br.ReadInt32();
                    sb.Append(" keyStrPoolSize=").Append(ksSize);
                    ms.Seek(ksSize - 8, SeekOrigin.Current);
                    int n = 0, hit = 0;
                    while (ms.Position < PackChunkPos + PackChunkSize) {
                        long chunkPos = ms.Position;
                        short chunkType = br.ReadInt16();
                        int hs = br.ReadInt16();
                        int chunkSize = br.ReadInt32();
                        if (chunkSize <= 0) { sb.Append("\n  !! chunkSize<=0 @").Append(chunkPos); break; }
                        n++;
                        if (chunkType == (short)RES_TYPE.RES_TABLE_TYPE_TYPE) {
                            byte tid = br.ReadByte();
                            byte tflags = br.ReadByte();
                            ms.Seek(2, SeekOrigin.Current);
                            int entryCount = br.ReadInt32();
                            int entryStart = br.ReadInt32();
                            if (tid == typeId) {
                                hit++;
                                long idxBase = chunkPos + hs;
                                uint entryIndic = 0xffffffff;
                                bool sparse = (tflags & 0x01) != 0, off16 = (tflags & 0x02) != 0;
                                if (!sparse && !off16) {
                                    ms.Seek(idxBase + entryId * 4, SeekOrigin.Begin);
                                    entryIndic = br.ReadUInt32();
                                } else if (sparse) {
                                    for (int s = 0; s < entryCount; s++) {
                                        ms.Seek(idxBase + s * 4, SeekOrigin.Begin);
                                        ushort si = br.ReadUInt16(); ushort so = br.ReadUInt16();
                                        if (si == entryId) { entryIndic = (uint)so * 4; break; }
                                    }
                                } else if (entryId < 0x10000) {
                                    ms.Seek(idxBase + (long)entryId * 2, SeekOrigin.Begin);
                                    ushort o16 = br.ReadUInt16();
                                    entryIndic = (o16 == 0xffff) ? 0xffffffff : (uint)o16 * 4;
                                }
                                if (hit <= 4 || entryIndic != 0xffffffff) {
                                    sb.Append("\n  TYPE@").Append(chunkPos).Append(" hs=").Append(hs)
                                      .Append(" size=").Append(chunkSize).Append(" flags=0x").Append(tflags.ToString("X2"))
                                      .Append(" entryCount=").Append(entryCount).Append(" entryStart=").Append(entryStart)
                                      .Append(" -> indic=").Append(entryIndic == 0xffffffff ? "NONE" : ("0x" + entryIndic.ToString("X")));
                                    if (entryIndic != 0xffffffff) {
                                        long ep = chunkPos + entryStart + entryIndic;
                                        if (ep + 16 <= resources.Length) {
                                            ms.Seek(ep + 11, SeekOrigin.Begin);
                                            byte dt = br.ReadByte();
                                            uint dv = br.ReadUInt32();
                                            sb.Append(" dataType=0x").Append(dt.ToString("X2")).Append(" data=0x").Append(dv.ToString("X8"));
                                        } else {
                                            sb.Append(" !! entryPos ").Append(ep).Append(" beyond EOF");
                                        }
                                    }
                                }
                            }
                        }
                        ms.Seek(chunkPos + chunkSize, SeekOrigin.Begin);
                    }
                    sb.Append("\n  scannedChunks=").Append(n).Append(" typeHits=").Append(hit);
                    try {
                        ApkResource rr = QuickSearchResource(id);
                        sb.Append("\n  QuickSearchResource -> Count=").Append(rr.Count);
                        for (int k = 0; k < rr.values.Count && k < 6; k++) {
                            sb.Append("\n    [" ).Append(k).Append("] = ").Append(rr.values[k] == null ? "(null)" : rr.values[k].ToString());
                        }
                    } catch (Exception ex) {
                        sb.Append("\n  QuickSearchResource EX: ").Append(ex.Message);
                    }
                }
            }
            return sb.ToString();
        }

        public override AppPackageReader.AppType Type {
            get {
                return AppType.AndroidApp;
            }
        }
        public override string AppName {
            get {
                return getAttribute(TagApplication, AttrLabel);
            }
        }
        public override string Version {
            get {
                return getAttribute(TagManifest, AttrVersionName);
            }
        }
        public override string Revision {
            get {
                return getAttribute(TagManifest, AttrVersionCode);
            }
        }
        public override string Publisher {
            get {
                string[] names = PackageName.Split(new char[] { '.' });
                string[] Slice = new List<string>(names).GetRange(0, names.Length - 1).ToArray();
                return string.Join(".", Slice);
            }
        }

        public override Bitmap Icon => getImage(TagApplication, AttrIcon, new Size(64, 64));

        public override string PackageName => getAttribute(TagManifest, AttrPackage);
        public bool Debuggable => getAttribute(TagApplication, AttrDebuggable) == ConstTrue;

        /// <summary>
        /// get a string from manifest.xml
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        public string getAttribute(string tag, string attr) {
            return QuickSearchManifestXml(tag, attr);
        }

        // get an icon with size
        public override Bitmap getIcon(Size size) {
            return getImage(TagApplication, AttrIcon, size);
        }
        /// <summary>
        /// Get a Image object from manifest and resources
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        public Bitmap getImage(string tag, string attr, Size size) {
            if (getFlag("Density") == null) {
                setFlag("Density", 1);
            }

            List<string> candidates;
            try {
                candidates = getImagePaths(tag, attr);
            } catch (Exception ex) {
                Log("Error happens when resolving candidates for <" + tag + ">.<" + attr + "> : " + ex.Message);
                candidates = new List<string>();
            }

            if (candidates.Count == 0) {
                Log("Cannot find image for <" + tag + ">.<" + attr + ">");
                throw new Exception("Cannot find image for <" + tag + ">.<" + attr + ">");
            }

            // FIX: 旧实现只尝试单个路径，一旦该路径是解析失败的 adaptive-icon / gradient /
            // objectAnimator 等 XML，整个图标就退回通用图标。
            // 现在按「期望质量」从高到低依次尝试候选路径，任一成功即返回。
            Exception lastError = null;
            foreach (string path in candidates) {
                try {
                    Bitmap b = getImage(path, size);
                    if (b != null) {
                        iconPath = path;
                        return b;
                    }
                    iconTrace.Append("candidate returned null: ").Append(path).Append("; ");
                } catch (Exception ex) {
                    lastError = ex;
                    Log("Image path : " + path);
                    Log("Error happens during extracting image, " + ex.Message);
                    iconTrace.Append("candidate failed: ").Append(path).Append(" -> ")
                             .Append(ex.Message.Replace("\r", " ").Replace("\n", " "))
                             .Append("; ");
                }
            }

            if (lastError != null) {
                Log("All icon candidates failed, last error: " + lastError.Message);
            }
            return null;
        }

        /// <summary>
        /// 解析 android:icon 等属性的候选资源路径，按「期望质量」从高到低排序。
        /// 首选自适应/矢量图标（SupportAdaptiveIcon 打开时），其余按真实像素面积排序。
        /// </summary>
        public List<string> getImagePaths(string tag, string attr) {
            if (getFlag("Density") == null) {
                setFlag("Density", 1);
            }
            string path = QuickSearchManifestXml(tag, attr);
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(path)) {
                return result;
            }

            ApkResource res = lastReferenceResource;
            if (res != null && res.Count > 1 && containsValue(res, path)) {
                foreach (string p in rankCandidates(res)) {
                    if (!result.Contains(p)) result.Add(p);
                }
            }
            if (!result.Contains(path)) {
                result.Insert(0, path);
            }
            return result;
        }

        private static bool containsValue(ApkResource res, string value) {
            for (int i = 0; i < res.values.Count; i++) {
                object v = res.values[i];
                if (v != null && v.ToString() == value) return true;
            }
            return false;
        }

        /// <summary>
        /// 按「清晰度期望值」给资源的各个 config 打分：位图看真实像素面积，XML 看密度。
        /// </summary>
        private List<string> rankCandidates(ApkResource r) {
            bool supAI = Utility.GetSetting("SupportAdaptiveIcon", "False") == "True";
            List<KeyValuePair<long, string>> scored = new List<KeyValuePair<long, string>>();

            for (int i = 0; i < r.configs.Count; i++) {
                object raw = (i < r.values.Count) ? r.values[i] : null;
                string value = (raw == null) ? "" : raw.ToString();
                if (string.IsNullOrEmpty(value)) continue;
                // "(0x7F0F0006)" 这类是解析失败的占位串，不能当路径用
                if (value.StartsWith("(")) continue;
                if (value.StartsWith("#")) continue;

                bool isXml = value.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
                if (!isXml) {
                    // FIX: 资源名混淆后扩展名不可信（可能是 .webp 里装 PNG，也可能无扩展名）。
                    // 这里用文件头判断是否二进制 XML，避免把 layout/menu 之类的 XML 当成位图候选
                    // 参与排序（它们无法解码，会白白占用优先级并抛异常）。
                    try {
                        ZipEntry xe = GetEntryOrdinal(value);
                        if (xe != null && xe.Size > 0) {
                            byte[] xh = ReadEntryHead(xe, 12);
                            isXml = SniffImageKind(xh) == "xml";
                        }
                    } catch { }
                }

                int density = 0;
                byte[] conf = r.configs[i];
                if (conf != null && conf.Length > (int)ResourceConfig.Density + 1) {
                    density = conf[(int)ResourceConfig.Density + 1] * 256 + conf[(int)ResourceConfig.Density];
                }

                long score;
                if (isXml) {
                    // 关闭自适应图标时，XML（自适应/矢量/layer-list）不作为候选
                    if (!supAI) continue;
                    score = 1000000L + density;
                } else {
                    int w, h;
                    if (tryProbeImageSize(value, out w, out h) && w > 0 && h > 0) {
                        score = (long)w * h;                        // 主判据：真实像素面积
                    } else {
                        score = (long)density * density;            // 兜底：密度平方
                    }
                }
                scored.Add(new KeyValuePair<long, string>(score, value));
            }

            scored.Sort(delegate (KeyValuePair<long, string> a, KeyValuePair<long, string> b) {
                return b.Key.CompareTo(a.Key);
            });

            List<string> outp = new List<string>();
            foreach (KeyValuePair<long, string> kv in scored) {
                if (!outp.Contains(kv.Value)) outp.Add(kv.Value);
            }
            return outp;
        }

        /// <summary>
        /// 只读取压缩包头几十字节，推断位图真实像素尺寸（不完整解码，开销极小）。
        /// 支持 PNG / JPEG / GIF / BMP / WebP(VP8、VP8L、VP8X)。
        /// </summary>
        public bool tryProbeImageSize(string path, out int width, out int height) {
            width = 0; height = 0;
            try {
                ZipEntry en = GetEntryOrdinal(path);
                if (en == null || en.Size <= 0) return false;
                int want = (int)Math.Min(en.Size, 8192);
                byte[] head = new byte[want];
                int got = 0;
                using (Stream s = zip.GetInputStream(en)) {
                    int r;
                    while (got < want && (r = s.Read(head, got, want - got)) > 0) got += r;
                }
                if (got < 16) return false;
                return ParseImageSize(head, got, out width, out height);
            } catch {
                width = 0; height = 0;
                return false;
            }
        }

        internal static bool ParseImageSize(byte[] b, int len, out int w, out int h) {
            w = 0; h = 0;

            // PNG: 89 50 4E 47 ... IHDR 在 8..24
            if (len >= 24 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) {
                w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                return w > 0 && h > 0;
            }
            // GIF: 逻辑屏幕宽高在 6..10（小端）
            if (len >= 10 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F') {
                w = b[6] | (b[7] << 8);
                h = b[8] | (b[9] << 8);
                return w > 0 && h > 0;
            }
            // BMP
            if (len >= 26 && b[0] == 'B' && b[1] == 'M') {
                w = b[18] | (b[19] << 8) | (b[20] << 16) | (b[21] << 24);
                h = b[22] | (b[23] << 8) | (b[24] << 16) | (b[25] << 24);
                return w > 0 && h != 0;
            }
            // JPEG: 需要扫描 SOFn
            if (len >= 4 && b[0] == 0xFF && b[1] == 0xD8) {
                int i = 2;
                while (i + 9 < len) {
                    if (b[i] != 0xFF) { i++; continue; }
                    byte m = b[i + 1];
                    if (m == 0xD8 || m == 0x01 || (m >= 0xD0 && m <= 0xD7)) { i += 2; continue; }
                    if (m == 0xDA || m == 0xD9) break;
                    int seg = (b[i + 2] << 8) | b[i + 3];
                    if (m >= 0xC0 && m <= 0xCF && m != 0xC4 && m != 0xC8 && m != 0xCC) {
                        h = (b[i + 5] << 8) | b[i + 6];
                        w = (b[i + 7] << 8) | b[i + 8];
                        return w > 0 && h > 0;
                    }
                    if (seg <= 0) break;
                    i += 2 + seg;
                }
                return false;
            }
            // WebP: RIFF....WEBP
            if (len >= 30 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
                && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') {
                string fourcc = Encoding.ASCII.GetString(b, 12, 4);
                if (fourcc == "VP8X") {
                    w = 1 + b[24] + (b[25] << 8) + (b[26] << 16);
                    h = 1 + b[27] + (b[28] << 8) + (b[29] << 16);
                    return w > 0 && h > 0;
                }
                if (fourcc == "VP8 ") {
                    w = (b[26] | (b[27] << 8)) & 0x3FFF;
                    h = (b[28] | (b[29] << 8)) & 0x3FFF;
                    return w > 0 && h > 0;
                }
                if (fourcc == "VP8L" && len >= 25) {
                    int b0 = b[21], b1 = b[22], b2 = b[23], b3 = b[24];
                    w = 1 + (((b1 & 0x3F) << 8) | b0);
                    h = 1 + (((b3 & 0x0F) << 10) | (b2 << 2) | ((b1 & 0xC0) >> 6));
                    return w > 0 && h > 0;
                }
            }
            return false;
        }

        /// <summary>
        /// 带 alpha 的 WebP 解码（BGRA 直出）。
        /// 旧实现用 WebPWrapper.Decode，内部固定 Format24bppRgb，透明区域会变成黑块。
        /// 失败时返回 null，由调用方回退到旧实现。
        /// </summary>
        private static Bitmap WebPDecodeWithAlpha(byte[] rawWebP) {
            if (rawWebP == null || rawWebP.Length < 16) return null;
            GCHandle pinned = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);
            Bitmap bmp = null;
            BitmapData bd = null;
            try {
                IntPtr p = pinned.AddrOfPinnedObject();
                int w, h;
                int ok = (IntPtr.Size == 8)
                    ? WebPGetInfo_x64(p, (UIntPtr)rawWebP.Length, out w, out h)
                    : WebPGetInfo_x86(p, (UIntPtr)rawWebP.Length, out w, out h);
                if (ok == 0 || w <= 0 || h <= 0) return null;

                bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                int size = bd.Stride * h;
                IntPtr res = (IntPtr.Size == 8)
                    ? WebPDecodeBGRAInto_x64(p, (UIntPtr)rawWebP.Length, bd.Scan0, size, bd.Stride)
                    : WebPDecodeBGRAInto_x86(p, (UIntPtr)rawWebP.Length, bd.Scan0, size, bd.Stride);
                if (res == IntPtr.Zero) return null;
                bmp.UnlockBits(bd);
                bd = null;
                return bmp;
            } catch {
                return null;
            } finally {
                if (bd != null && bmp != null) { try { bmp.UnlockBits(bd); } catch { } }
                if (pinned.IsAllocated) pinned.Free();
            }
        }

        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetInfo")]
        private static extern int WebPGetInfo_x86(IntPtr data, UIntPtr data_size, out int width, out int height);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetInfo")]
        private static extern int WebPGetInfo_x64(IntPtr data, UIntPtr data_size, out int width, out int height);

        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRAInto")]
        private static extern IntPtr WebPDecodeBGRAInto_x86(IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRAInto")]
        private static extern IntPtr WebPDecodeBGRAInto_x64(IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

        /// <summary>
        /// return the bitmap of a image file path located in apk zip
        /// accepting .png, adaptive-icon or vectordrawable
        /// </summary>
        /// <param name="path">png or xml file within apk zip</param>
        /// <returns>Bitmap or </returns>
        public Bitmap getImage(string path, Size size) {
            ZipEntry iconz;
            // FIX: FindEntry 返回的是「索引」，索引 0 也是合法条目；
            // 旧实现用 > 0 判断，导致位于压缩包第 0 条的图标被误判为不存在。
            if ((iconz = GetEntryOrdinal(path)) != null) {
                string lower = path.ToLowerInvariant();
                string byExt = null;
                if (lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") ||
                    lower.EndsWith(".gif") || lower.EndsWith(".tif") || lower.EndsWith(".tiff") ||
                    lower.EndsWith(".bmp")) {
                    byExt = "bitmap";
                } else if (lower.EndsWith(".webp")) {
                    byExt = "webp";
                } else if (lower.EndsWith(".xml")) {
                    byExt = "xml";
                }

                // FIX: 资源混淆工具（AndResGuard / NP管理器 / MT管理器 等）会把 res/ 下的文件名
                // 改成随机名，并且**扩展名与真实内容不一致**。实测 vcam Ultra.apk / Apex顶峰VCAM
                // 里 res/*.webp 的内容其实是 PNG，res/*.webp 里还有 JPEG；
                // 而部分条目干脆没有扩展名（NP管理器里 android:icon 指向的条目名就叫 "ۤۦ"）。
                // 旧实现只信扩展名，于是把 PNG 丢给 WebP 解码器 → 报 "Can´t get information of WebP"
                // → 整个图标解析失败。这里改为「内容魔数优先，扩展名兜底」。
                string kind = null;
                string sniffed = SniffImageKind(ReadEntryHead(iconz, 16));
                if (sniffed == "png" || sniffed == "jpg" || sniffed == "gif" || sniffed == "bmp") {
                    kind = "bitmap";
                } else if (sniffed != null) {
                    kind = sniffed;
                }
                if (kind != null) {
                    if (byExt != null && byExt != kind) {
                        iconTrace.Append("header=").Append(kind).Append(" but extension=").Append(byExt)
                                 .Append(" (renamed resource): ").Append(path).Append("; ");
                    } else if (byExt == null) {
                        iconTrace.Append("sniffed ").Append(kind)
                                 .Append(" from header (entry has no useful extension): ").Append(path).Append("; ");
                    }
                } else {
                    kind = byExt;   // 未能识别文件头时退回扩展名判断
                }

                if (kind == "bitmap") {
                    Bitmap src = (Bitmap)Image.FromStream(zip.GetInputStream(iconz));
                    iconSourceSize = new Size(src.Width, src.Height);
                    return Utility.ResizeBitmap(src, size);
                } else if (kind == "webp") {
                    byte[] bytes = new BinaryReader(zip.GetInputStream(iconz)).ReadBytes((int)iconz.Size);
                    int pw, ph;
                    if (ParseImageSize(bytes, bytes.Length, out pw, out ph))
                        iconSourceSize = new Size(pw, ph);
                    // FIX: 旧的 WebP.Decode 固定用 24bpp，丢掉了 alpha，透明背景会变成黑块。
                    // 优先走带 alpha 的 BGRA 解码，失败再退回旧实现。
                    Bitmap decoded = WebPDecodeWithAlpha(bytes);
                    if (decoded == null) {
                        WebP webp = new WebP();
                        decoded = webp.Decode(bytes);
                    }
                    return Utility.ResizeBitmap(decoded, size);
                } else if (kind == "xml") {
                    XmlDocument doc = ExtractCompressedXml(path);
                    if (doc == null || doc.FirstChild == null) {
                        throw new Exception("cannot parse xml image " + path);
                    }
                    iconTrace.Append("xml tag=").Append(doc.FirstChild.Name).Append(" path=").Append(path).Append("; ");
                    if (doc.FirstChild.Name == "adaptive-icon") {
                        return parseAdaptiveIcon(doc, size);
                    } else if (doc.FirstChild.Name == "vector") { // this is a vectordrawable
                        return parseVectorDrawable(doc, size);
                    } else if (doc.FirstChild.Name == "shape") {
                        return parseShape(doc, size);
                    } else if (doc.FirstChild.Name == "layer-list") {
                        return parseLayerDrawable(doc, size);
                    } else {
                        throw new Exception("unsupported image file " + path + " with tag " + doc.FirstChild.Name);
                    }
                }
            }
            return null;
        }

        /// <summary>读取条目前若干字节（用于文件头嗅探），失败返回 null。</summary>
        private byte[] ReadEntryHead(ZipEntry en, int count) {
            try {
                if (en.Size <= 0) return null;
                int want = (int)Math.Min(en.Size, count);
                byte[] head = new byte[want];
                int got = 0;
                using (Stream s = zip.GetInputStream(en)) {
                    int r;
                    while (got < want && (r = s.Read(head, got, want - got)) > 0) got += r;
                }
                if (got < want) {
                    byte[] trim = new byte[got];
                    Array.Copy(head, trim, got);
                    return trim;
                }
                return head;
            } catch {
                return null;
            }
        }

        /// <summary>按文件头判断图片类型（扩展名缺失/错误时使用）。</summary>
        internal static string SniffImageKind(byte[] h) {
            if (h == null || h.Length < 12) return null;
            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return "png";
            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return "jpg";
            if (h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38) return "gif";
            if (h[0] == 0x42 && h[1] == 0x4D) return "bmp";
            if (h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
                h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50) return "webp";
            // 二进制 XML：ResChunk_header { type=0x0003 RES_XML_TYPE, headerSize=0x0008 }
            if (h[0] == 0x03 && h[1] == 0x00 && h[2] == 0x08 && h[3] == 0x00) return "xml";
            return null;
        }

        private Bitmap parseLayerDrawable(XmlNode node, Size size) {
            try {
                XmlNodeList nl = node.SelectNodes("/layer-list/item");
                List<Bitmap> bitmapList = new List<Bitmap>(nl.Count);
                foreach (XmlElement e in nl) {
                    if (e.HasAttribute("drawable")) {
                        Bitmap b = getImage(e.GetAttribute("drawable"), size);
                        bitmapList.Add(b);
                    } else {
                        XmlElement shape = (XmlElement)e.SelectSingleNode("shape");
                        if (shape != null) {
                            bitmapList.Add(parseShape(shape, size));
                        } else {
                            throw new Exception("unrecorgnized element in layer-list.");
                        }
                    }
                }
                Bitmap bmp = new Bitmap(size.Width, size.Height);
                Graphics g = Graphics.FromImage(bmp);
                bool drawn = false;
                foreach (Bitmap b in bitmapList) {
                    if (b == null) continue;
                    g.DrawImage(Utility.ResizeBitmap(b, new Size(bmp.Width, bmp.Height)), 0, 0);
                    drawn = true;
                }
                g.Dispose();
                if (!drawn) {
                    bmp.Dispose();
                    iconTrace.Append("layer-list drew nothing; ");
                    return null;
                }
                return bmp;
            } catch (Exception ex) {
                throw new Exception(ex.Message + "\nError happens during parsing layer-list: " + node.InnerXml);
            }
        }

        private Bitmap parseVectorDrawable(XmlNode node, Size size) {
            try {
                XmlElement vector = (XmlElement)node.SelectSingleNode("/vector");
                float viewportWidth = 0, viewportHeight = 0;
                int width = 0, height = 0;
                GraphicsUnit units = GraphicsUnit.Display;
                if (vector.HasAttribute("viewportWidth")) {
                    viewportWidth = toFloat(vector.GetAttribute("viewportWidth"), 0f);
                }
                if (vector.HasAttribute("viewportHeight")) {
                    viewportHeight = toFloat(vector.GetAttribute("viewportHeight"), 0f);
                    if (viewportWidth == 0) viewportWidth = viewportHeight;
                } else {
                    viewportHeight = viewportWidth;
                }
                if (vector.HasAttribute("width")) {
                    // FIX: 旧实现在内层 catch 里再次用 int.Parse 解析 "1.5dp" 这类小数值，
                    // 会抛出 FormatException 并让整个矢量图标解析失败。
                    string ori = vector.GetAttribute("width").Trim();
                    string numPart = ori;
                    string strunit = "";
                    if (numPart.Length > 2) {
                        strunit = numPart.Substring(numPart.Length - 2).ToLowerInvariant();
                        if (strunit == "dp" || strunit == "in" || strunit == "mm"
                            || strunit == "px" || strunit == "sp" || strunit == "pt") {
                            numPart = numPart.Substring(0, numPart.Length - 2);
                        } else {
                            strunit = "";
                        }
                    }
                    width = (int)Math.Round(toFloat(numPart, 0f));
                    switch (strunit) {
                        case "dp":
                            units = GraphicsUnit.Display;
                            break;
                        case "in":
                            units = GraphicsUnit.Inch;
                            break;
                        case "mm":
                            units = GraphicsUnit.Millimeter;
                            break;
                        case "px":
                            units = GraphicsUnit.Pixel;
                            break;
                        case "sp":
                            units = GraphicsUnit.World;
                            break;
                        case "pt":
                            units = GraphicsUnit.Point;
                            break;
                        default:
                            units = GraphicsUnit.Display;
                            break;
                    }
                    //Not finished yet
                }
                if (viewportWidth < 1) viewportWidth = (width > 0) ? width : 24;
                if (viewportHeight < 1) viewportHeight = (viewportWidth > 0) ? viewportWidth : 24;
                Bitmap b = new Bitmap((int)viewportWidth, (int)viewportHeight);
                using (Graphics g = Graphics.FromImage(b)) {
                    XmlElement group = (XmlElement)vector.SelectSingleNode("group");
                    XmlNodeList nl = (group != null) ? group.SelectNodes("path") :
                                                      vector.SelectNodes("path");
                    foreach (XmlElement elem in nl) {
                        string pathdata = elem.GetAttribute("pathData");
                        GraphicsPath gpath = VectorDrawableRender.Convert2Path(pathdata);
                        Brush fill = null;
                        if (elem.HasAttribute("fillColor")) {
                            string fillcolor = elem.GetAttribute("fillColor");
                            if (fillcolor.EndsWith(".xml")) {//gradien
                                fill = parseGradient(fillcolor);
                            } else {
                                fill = new SolidBrush(stringToColor(elem.GetAttribute("fillColor")));
                            }
                        } else {
                            fill = new SolidBrush(System.Drawing.Color.Black);
                        }
                        g.FillPath(fill, gpath);
                        //g.DrawPath(new Pen(fill, 2), path);                    
                    }
                }
                return Utility.ResizeBitmap(b,size);
            } catch (Exception ex) {
                throw new Exception(ex.Message + "\nError happens during parsing vectordrawable: " + node.InnerXml);
            }
        }
        
        private Bitmap parseAdaptiveIcon(XmlNode node, Size size) {
            /// Get adaptive - icon
            /// 
            /*
            <?xml version="1.0" encoding="utf-8"?>
            <adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
            <background android:drawable="@drawable/ic_launcher_background" />
            <foreground android:drawable="@drawable/ic_launcher_foreground" />
            </adaptive-icon>
            */
            try {
                XmlElement elem = (XmlElement)node.SelectSingleNode("/adaptive-icon/background");
                Bitmap b = null, f = null;
                Bitmap bmp = new Bitmap(size.Width, size.Height);
                Graphics g = Graphics.FromImage(bmp);
                bool drawn = false;
                if (elem != null && elem.HasAttribute("drawable")) {
                    b = getImage(elem.GetAttribute("drawable"), size);
                    if (b != null) {
                        g.DrawImage(b, 0, 0);
                        drawn = true;
                    } else {
                        Color c = stringToColor(elem.GetAttribute("drawable"));
                        if (c.A != 0) {
                            g.FillRectangle(new SolidBrush(c), 0, 0, bmp.Width, bmp.Height);
                            drawn = true;
                        }
                    }
                }
                elem = (XmlElement)node.SelectSingleNode("/adaptive-icon/foreground");
                if (elem != null && elem.HasAttribute("drawable")) {
                    f = getImage(elem.GetAttribute("drawable"), size);
                    if (f != null) {
                        g.DrawImage(f, 0, 0);
                        drawn = true;
                    } else {
                        Color c = stringToColor(elem.GetAttribute("drawable"));
                        if (c.A != 0) {
                            g.FillRectangle(new SolidBrush(c), 0, 0, bmp.Width, bmp.Height);
                            drawn = true;
                        }
                    }
                }
                g.Dispose();
                // FIX: 一个都没画出来时返回 null（而不是全透明位图），
                // 这样上层可以继续尝试下一个候选（例如 192x192 的 PNG），图标不会变成空白。
                if (!drawn) {
                    bmp.Dispose();
                    iconTrace.Append("adaptive-icon drew nothing; ");
                    return null;
                }
                return bmp;
            } catch (Exception ex) {
                throw new Exception(ex.Message + "\nError happening during parse AdaptiveIcon.");
            }
        }

        private Brush parseGradient(XmlNode node) {
            try {
                XmlElement gradient = (XmlElement)node.FirstChild;
                if (gradient == null) {
                    throw new Exception("empty gradient xml");
                }

                // Android <gradient> 允许把颜色写在 <item color offset> 里而不写 startColor/endColor，
                // 旧实现只读 startColor/endColor，于是 stringToColor("") 抛 FormatException。
                XmlNodeList itemNodes = node.SelectNodes("item");
                List<Color> itemColors = new List<Color>();
                List<float> itemOffsets = new List<float>();
                foreach (XmlElement p in itemNodes) {
                    if (!p.HasAttribute("color")) continue;
                    itemColors.Add(stringToColor(p.GetAttribute("color")));
                    itemOffsets.Add(toFloat(p.GetAttribute("offset"), 0f));
                }

                Color fallbackStart = itemColors.Count > 0 ? itemColors[0] : Color.Transparent;
                Color fallbackEnd = itemColors.Count > 0 ? itemColors[itemColors.Count - 1] : Color.Transparent;

                int type;
                if (!int.TryParse(gradient.GetAttribute("type"), NumberStyles.Integer, CultureInfo.InvariantCulture, out type))
                    type = (int)gradientType.linear;

                if (type == (int)gradientType.linear) {
                    Color startC = gradient.HasAttribute("startColor")
                        ? stringToColor(gradient.GetAttribute("startColor")) : fallbackStart;
                    Color endC = gradient.HasAttribute("endColor")
                        ? stringToColor(gradient.GetAttribute("endColor")) : fallbackEnd;

                    float startX = toFloat(gradient.GetAttribute("startX"), 0f);
                    float startY = toFloat(gradient.GetAttribute("startY"), 0f);
                    float endX = toFloat(gradient.GetAttribute("endX"), 0f);
                    float endY = toFloat(gradient.GetAttribute("endY"), 0f);

                    if (Math.Abs(startX - endX) < 0.001f && Math.Abs(startY - endY) < 0.001f) {
                        // 起止点重合会让 LinearGradientBrush 抛 ArgumentException，这里退化成纯色
                        return new SolidBrush(endC);
                    }

                    LinearGradientBrush lg = new LinearGradientBrush(
                        new PointF(startX, startY), new PointF(endX, endY), startC, endC);

                    if (itemColors.Count > 2) {
                        ColorBlend blend = new ColorBlend(itemColors.Count);
                        blend.Colors = itemColors.ToArray();
                        float[] pos = new float[itemColors.Count];
                        for (int i = 0; i < itemOffsets.Count && i < pos.Length; i++) {
                            float o = itemOffsets[i];
                            pos[i] = (o < 0f) ? 0f : (o > 1f ? 1f : o);
                        }
                        for (int i = 1; i < pos.Length; i++) {
                            if (pos[i] < pos[i - 1]) pos[i] = pos[i - 1]; // ColorBlend 要求单调不减
                        }
                        blend.Positions = pos;
                        lg.InterpolationColors = blend;
                    }
                    return lg;
                } else if (type == (int)gradientType.radial) {
                    Color c = itemColors.Count > 0 ? itemColors[itemColors.Count - 1] : Color.Transparent;
                    if (!gradient.HasAttribute("centerX") && !gradient.HasAttribute("gradientRadius")) {
                        return new SolidBrush(c);
                    }
                    // 径向渐变暂不做完整建模，退回末位色标（与旧行为一致，但不再抛异常）
                    return new SolidBrush(c);
                } else if (type == (int)gradientType.sweep) {
                    return null;
                } else {
                    throw new Exception("Unknow gradien type when parsing gradient xml");
                }
            } catch (Exception ex) {
                throw new Exception(ex.Message + "\nError happend during parse Gradient " + node.InnerXml);
            }
        }

        private Brush parseGradient(string xml) {
            try {
                XmlDocument doc = ExtractCompressedXml(xml);
                if (doc.FirstChild.Name == "gradient") {
                    return parseGradient(doc);
                } else {
                    throw new Exception("Doesn't find a gradient XML");
                }
            } catch (Exception ex) {
                throw new Exception(ex.Message + "\nError happens during parsing gradient xml " + xml);
            }
        }

        private Bitmap parseShape(XmlNode shapeNode, Size size) {
            try {
                Bitmap b;
                XmlElement eShape;
                if (shapeNode.NodeType == XmlNodeType.Document)
                    eShape = ((XmlDocument)shapeNode).DocumentElement;
                else
                    eShape = (XmlElement)shapeNode;
                shapeType type = (shapeType)int.Parse(eShape.GetAttribute("shape"));
                XmlElement eSize = (XmlElement)shapeNode.SelectSingleNode("size");
                if (eSize != null)
                    b = new Bitmap(Math.Max(1, (int)Math.Round(toFloat(stripUnit(eSize.GetAttribute("width")), size.Width))),
                                   Math.Max(1, (int)Math.Round(toFloat(stripUnit(eSize.GetAttribute("height")), size.Height))));
                else {
                    b = new Bitmap(size.Width, size.Height);
                }
                Graphics g = Graphics.FromImage(b);
                GraphicsPath p = new GraphicsPath();
                Brush brush;
                XmlElement ebrush = (XmlElement)eShape.SelectSingleNode("solid");
                if (ebrush != null) {
                    brush = new SolidBrush(stringToColor(ebrush.GetAttribute("color")));
                } else {
                    if ((ebrush = (XmlElement)eShape.SelectSingleNode("gradient")) != null) {
                        brush = parseGradient(ebrush);
                    } else
                        brush = new SolidBrush(Color.Black);
                }
                if (type == shapeType.rectangle) {
                    XmlElement corners = (XmlElement)eShape.SelectSingleNode("corners");
                    if (corners != null) {
                        //ToDo, support round corner
                    }
                    p.AddRectangle(new RectangleF(0, 0, b.Width, b.Height));
                } else if (type == shapeType.oval) {
                    p.AddEllipse(0, 0, b.Width, b.Height);
                } else if (type == shapeType.line) {
                    // todo: support line

                } else if (type == shapeType.ring) {
                    // todo: support ring
                } else {
                    throw new Exception("Unsupported shape type: " + (int)type);
                }
                g.FillPath(brush, p);
                return Utility.ResizeBitmap(b, size);
            } catch (Exception ex) {
                throw new Exception(ex.Message + "\nError happending during parsing shape:" + shapeNode.InnerXml);
            }
        }

        private Color stringToColor(string color) {
            // FIX: 旧实现直接 uint.Parse，遇到空串（Android 允许省略 startColor/endColor）、
            // 资源引用、或 #RRGGBB 这类短写法都会抛 FormatException，
            // 结果整个 adaptive-icon/vector 解析失败，图标退回通用图标。
            if (string.IsNullOrEmpty(color)) return Color.Transparent;
            color = color.Trim();
            if (color.Length == 0) return Color.Transparent;

            if (color[0] == '@' || color.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                return Color.Transparent;   // 资源引用或另一个 xml，不是颜色字面量

            if (color[0] == '#') {
                string hex = color.Substring(1);
                uint v;
                switch (hex.Length) {
                    case 3: // #RGB -> #RRGGBB
                        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) {
                            uint r = (v >> 8) & 0xF, g = (v >> 4) & 0xF, b = v & 0xF;
                            return Color.FromArgb(255, (int)(r * 17), (int)(g * 17), (int)(b * 17));
                        }
                        break;
                    case 4: // #ARGB
                        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) {
                            uint a = (v >> 12) & 0xF, r = (v >> 8) & 0xF, g = (v >> 4) & 0xF, b = v & 0xF;
                            return Color.FromArgb((int)(a * 17), (int)(r * 17), (int)(g * 17), (int)(b * 17));
                        }
                        break;
                    case 6: // #RRGGBB -> 不透明
                        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v))
                            return Color.FromArgb(255, (int)((v >> 16) & 0xFF), (int)((v >> 8) & 0xFF), (int)(v & 0xFF));
                        break;
                    case 8: // #AARRGGBB
                        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v))
                            return Color.FromArgb((int)v);
                        break;
                    default:
                        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v))
                            return Color.FromArgb((int)v);
                        break;
                }
                return Color.Transparent;
            }

            uint plain;
            if (uint.TryParse(color, NumberStyles.Integer, CultureInfo.InvariantCulture, out plain))
                return Color.FromArgb((int)plain);
            return Color.Transparent;
        }

        private static float toFloat(string s, float def) {
            float f;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
            return def;
        }

        /// <summary>去掉 Android 尺寸单位后缀（dp/sp/px/mm/in/pt）。</summary>
        private static string stripUnit(string s) {
            if (string.IsNullOrEmpty(s)) return "0";
            s = s.Trim();
            if (s.Length <= 2) return s;
            string u = s.Substring(s.Length - 2).ToLowerInvariant();
            if (u == "dp" || u == "sp" || u == "px" || u == "mm" || u == "in" || u == "pt")
                return s.Substring(0, s.Length - 2);
            return s;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public string QuickSearchManifestXml(string tag, string attribute) {
            if (manifest == null || manifest.Length == 0) {   // split apk / 损坏包
                iconTrace.Append("no AndroidManifest.xml; ");
                return "";
            }
            return QuickSearchCompressedXml(manifest, tag, attribute);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xml">xml byte stream</param>
        /// <param name="xpath"></param>
        /// <param name="attribute"></param>
        /// <param name="sub"></param>
        /// <returns></returns>
        private string QuickSearchCompressedXml(byte[] xml, string xpath, string attribute) {
            using (MemoryStream ms = new MemoryStream(xml))
            using (BinaryReader br = new BinaryReader(ms)) {
                Log("Search xml for " + xpath + " " + attribute);
                string[] pathl = xpath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                ms.Seek(8, SeekOrigin.Begin); // skip header, no doubt it's an xml chunk
                ApkResource result = new ApkResource(0);
                int tagDepth = 0;
                int matchDepth = 0;

                // XML_START_ELEMENT CHUNK
                while (ms.Position < ms.Length) {
                    long chunkPos = ms.Position;
                    RES_TYPE chunkType = (RES_TYPE)br.ReadInt16();
                    short headerSize = br.ReadInt16();
                    int chunkSize = br.ReadInt32();
                    if (chunkType == RES_TYPE.RES_XML_START_ELEMENT_TYPE) {
                        tagDepth++;

                        ms.Seek(8 + 4, SeekOrigin.Current); // skip line number & comment / namespace
                        string tag_s = QuickSearchCompressedXmlStringPoolAndResMap(xml, br.ReadUInt32());
                        if (tagDepth <= pathl.Length && tag_s.ToUpper() == pathl[tagDepth - 1].ToUpper()) {
                            matchDepth++;

                            if (matchDepth == pathl.Length) { // match, read attributes
                                int attributeStart = br.ReadInt16();
                                int attributeSize = br.ReadInt16();
                                int attributeCount = br.ReadInt16();
                                for (int i = 0; i < attributeCount; i++) {
                                    int offset = headerSize + attributeStart + attributeSize * i + 4;
                                    if (offset >= chunkSize) { // Error: comes to out of chunk
                                        throw new Exception("Out of Chunk when processing tag " + tag_s);
                                    }
                                    ms.Seek(chunkPos + offset, SeekOrigin.Begin); // ignore the ns                            
                                    uint ind = br.ReadUInt32();
                                    string name = QuickSearchCompressedXmlStringPoolAndResMap(xml, ind);
                                    if (name.ToUpper() == attribute.ToUpper()) {
                                        ms.Seek(4 + 2 + 1, SeekOrigin.Current); // skip rawValue/size/0/
                                        DATA_TYPE dataType = (DATA_TYPE)br.ReadByte();
                                        uint data = br.ReadUInt32();
                                        return convertData(xml, dataType, data);
                                    }
                                }
                            }
                        }
                    } else if (chunkType == RES_TYPE.RES_XML_END_ELEMENT_TYPE) {
                        if (matchDepth == tagDepth) {
                            matchDepth--;
                        }
                        tagDepth--;
                    }
                    ms.Seek(chunkPos + chunkSize, SeekOrigin.Begin);
                }
                return "";
            }
        }

        public XmlDocument ExtractCompressedXml(string path) {
            try {
                ZipEntry en = GetEntryOrdinal(path);
                if (en == null) {   // FIX: 旧实现不判空，GetInputStream(null) 抛 ArgumentNullException
                    throw new Exception("zip entry not found: " + path);
                }
                byte[] bytes = new BinaryReader(zip.GetInputStream(en)).ReadBytes((int)en.Size);
                return ExtractCompressedXml(bytes);
            } catch (Exception ex) {
                throw new Exception(ex.Message + "\nError happens during dump " + path);
            }
        }

        private XmlDocument ExtractCompressedXml(byte[] bytes) {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader br = new BinaryReader(ms)) {
                long chunkPos = ms.Position;
                RES_TYPE chunkType = (RES_TYPE)br.ReadInt16();
                short headerSize = br.ReadInt16();
                int chunkSize = br.ReadInt32();
                ms.Seek(chunkPos + headerSize, SeekOrigin.Begin);

                XmlDocument doc = new XmlDocument();
                XmlNode currentNode = doc;
                XmlNamespaceManager nm = new XmlNamespaceManager(doc.NameTable);

                try {
                    // XML_START_ELEMENT CHUNK
                    while (ms.Position < ms.Length) {
                        chunkPos = ms.Position;
                        chunkType = (RES_TYPE)br.ReadInt16();
                        headerSize = br.ReadInt16();
                        chunkSize = br.ReadInt32();
                        ms.Seek(chunkPos + headerSize, SeekOrigin.Begin);
                        if (chunkType == RES_TYPE.RES_XML_START_NAMESPACE_TYPE) {                            
                            string prefix = QuickSearchStringPool(bytes, br.ReadUInt32());
                            string uri = QuickSearchStringPool(bytes, br.ReadUInt32());
                            nm.AddNamespace(prefix, uri);
                        } else if (chunkType == RES_TYPE.RES_XML_CDATA_TYPE) {
                            string cdatastr = QuickSearchStringPool(bytes, br.ReadUInt16());
                            XmlCDataSection cdata = doc.CreateCDataSection(cdatastr);
                            currentNode.AppendChild(cdata);
                        } else if (chunkType == RES_TYPE.RES_XML_START_ELEMENT_TYPE) {
                            string ns = QuickSearchCompressedXmlStringPoolAndResMap(bytes, br.ReadUInt32());
                            string tag_s = QuickSearchCompressedXmlStringPoolAndResMap(bytes, br.ReadUInt32());
                            XmlElement currentElem = doc.CreateElement(tag_s, ns);
                            currentNode.AppendChild(currentElem);
                            currentNode = currentElem;

                            int attributeStart = br.ReadInt16();
                            int attributeSize = br.ReadInt16();
                            int attributeCount = br.ReadInt16();
                            for (int i = 0; i < attributeCount; i++) {
                                int offset = headerSize + attributeStart + attributeSize * i + 4;
                                ms.Seek(chunkPos + offset, SeekOrigin.Begin);
                                uint ind = br.ReadUInt32();
                                string name = QuickSearchCompressedXmlStringPoolAndResMap(bytes, ind);
                                ms.Seek(4 + 2 + 1, SeekOrigin.Current); // skip rawValue/size/0/
                                DATA_TYPE dataType = (DATA_TYPE)br.ReadByte();
                                uint data = br.ReadUInt32();
                                string val = convertData(bytes, dataType, data);
                                currentElem.SetAttribute(name, val);
                            }
                        } else if (chunkType == RES_TYPE.RES_XML_END_ELEMENT_TYPE) {
                            currentNode = currentNode.ParentNode;
                        }
                        ms.Seek(chunkPos + chunkSize, SeekOrigin.Begin);
                    }
                    return doc;
                } catch (Exception ex) {
                    Log(doc.InnerXml);
                    throw new Exception(ex.Message + "\nError happens during dump xml file @ bytes " + ms.Position.ToString());
                }
            }
        }

        private string convertData(byte[] bytes, DATA_TYPE type, UInt32 data) {
            switch (type) {
                case DATA_TYPE.TYPE_STRING:
                    return QuickSearchStringPool(bytes, data);
                case DATA_TYPE.TYPE_REFERENCE:
                case DATA_TYPE.TYPE_DYNAMIC_REFERENCE:
                    try {
                        ApkResource r = QuickSearchResource((UInt32)data);
                        lastReferenceResource = r;
                        // FIX: 资源解析不到时 r.Count == 0，旧实现访问 values[0] 抛异常，
                        // 然后把 "(0x7F0F0006)" 当成图标路径返回，导致找不到任何文件。
                        if (r.Count == 0) {
                            Log("resource not found for ID:0x" + data.ToString("X8"));
                            lastReferenceResource = null;
                            return "";
                        }
                        int ind = 0;
                        if (r.Count > 1) {
                            ind = applyFilter(r);
                            if (ind < 0 || ind >= r.values.Count) ind = 0;
                        }
                        object v = r.values[ind];
                        return (v == null) ? "" : v.ToString();
                    } catch (Exception ex) {
                        Log("Error happen when finding resource with ID:0x" + data.ToString("X8") + ex.Message);
                        lastReferenceResource = null;
                        return "";
                    }
                case DATA_TYPE.TYPE_INT_BOOLEAN:
                    return (data == 0) ? ConstTrue : ConstFalse;
                case DATA_TYPE.TYPE_DIMENSION:
                    COMPLEX_TYPE t = (COMPLEX_TYPE)(data & 0xff);
                    string unit = (t == COMPLEX_TYPE.COMPLEX_UNIT_DIP) ? "dp" :
                                  (t == COMPLEX_TYPE.COMPLEX_UNIT_IN) ? "in" :
                                  (t == COMPLEX_TYPE.COMPLEX_UNIT_MM) ? "mm" :
                                  (t == COMPLEX_TYPE.COMPLEX_UNIT_PX) ? "px" :
                                  (t == COMPLEX_TYPE.COMPLEX_UNIT_SP) ? "sp" :
                                  (t == COMPLEX_TYPE.COMPLEX_UNIT_PT) ? "pt" : "";
                    return (data >> 8).ToString() + unit;
                case DATA_TYPE.TYPE_FLOAT:
                    float f = BitConverter.ToSingle(BitConverter.GetBytes(data), 0);
                    return f.ToString();
                case DATA_TYPE.TYPE_INT_COLOR_ARGB8:
                case DATA_TYPE.TYPE_INT_COLOR_ARGB4:
                case DATA_TYPE.TYPE_INT_COLOR_RGB4:
                case DATA_TYPE.TYPE_INT_COLOR_RGB8:
                    string hex = data.ToString("X8");
                    return "#" + hex;
                default:
                    return data.ToString();
            }
        }

        private string QuickSearchCompressedXmlStringPoolAndResMap(byte[] xml, uint id) {
            if (id == 0xffffffff) return "";
            string result = QuickSearchStringPool(xml, id);
            if (result == "") {
                result = QuickSearchCompressedXMlResMap(xml, id);
                if (result == "") result = "(0x" + id.ToString("X8") + ")";
            }
            return result;
        }

        private string QuickSearchStringPool(byte[] bytes, uint id) {
            if (id == 0xffffffff) return "";
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader br = new BinaryReader(ms)) {
                RES_TYPE chunkType = (RES_TYPE)br.ReadInt16();
                short headerSize = br.ReadInt16();
                ms.Seek(headerSize, SeekOrigin.Begin);

                while (ms.Position < ms.Length) {
                    long chunkPos = ms.Position;
                    chunkType = (RES_TYPE)br.ReadInt16();
                    headerSize = br.ReadInt16();
                    int chunkSize = br.ReadInt32();
                    if (chunkType == RES_TYPE.RES_STRING_POOL_TYPE) {
                        int stringcount = br.ReadInt32();
                        int stylecount = br.ReadInt32();
                        int flags = br.ReadInt32();
                        bool isUTF_8 = (flags & (1 << 8)) != 0;
                        int stringStart = br.ReadInt32();
                        ms.Seek(4 + id*4, SeekOrigin.Current);
                        int stringPos = br.ReadInt32();
                        ms.Seek(chunkPos + stringStart + stringPos, SeekOrigin.Begin);
                        if (isUTF_8) {
                            int u16len = br.ReadByte(); // u16len
                            if ((u16len & 0x80) != 0) {// larger than 128
                                u16len = ((u16len & 0x7F) << 8) + br.ReadByte();
                            }

                            int u8len = br.ReadByte(); // u8len
                            if ((u8len & 0x80) != 0) {// larger than 128
                                u8len = ((u8len & 0x7F) << 8) + br.ReadByte();
                            }
                            return Encoding.UTF8.GetString(br.ReadBytes(u8len));
                        } else // UTF_16
                        {
                            int u16len = br.ReadUInt16();
                            if ((u16len & 0x8000) != 0) {// larger than 32768
                                u16len = ((u16len & 0x7FFF) << 16) + br.ReadUInt16();
                            }

                            return Encoding.Unicode.GetString(br.ReadBytes(u16len * 2));
                        }
                    } else {
                        ms.Seek(chunkPos + chunkSize, SeekOrigin.Begin);
                    }
                }
                return "";
            }
        }

        private string QuickSearchCompressedXMlResMap(byte[] xml, uint id) {
            if (id == 0xffffffff) return "";
            using (MemoryStream ms = new MemoryStream(xml))
            using (BinaryReader br = new BinaryReader(ms)) {
                RES_TYPE chunkType = (RES_TYPE)br.ReadInt16();
                short headerSize = br.ReadInt16();
                ms.Seek(headerSize, SeekOrigin.Begin);

                while (ms.Position < ms.Length) {
                    long chunkPos = ms.Position;
                    chunkType = (RES_TYPE)br.ReadInt16();
                    headerSize = br.ReadInt16();
                    int chunkSize = br.ReadInt32();

                    if (chunkType == RES_TYPE.RES_XML_RESOURCE_MAP_TYPE) {
                        //Resource map
                        ms.Seek(id * 4, SeekOrigin.Current);
                        string result = Enum.GetName(typeof(R_attr), br.ReadUInt32());
                        if (result != null)
                            return result;
                        else
                            return "";
                    } else {
                        ms.Seek(chunkPos + chunkSize, SeekOrigin.Begin);
                    }
                }
                return "";
            }
        }

        /// <summary>
        /// Get culture info from string
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private CultureInfo getCulture(byte[] code) {
            string language;
            string country;
            byte[] decode;
            if (code[1] > 0x80) { // ISO-639-2
                decode = new byte[3];
                decode[0] = (byte)(code[0] & 0x1F);
                decode[1] = (byte)(((code[1] & 0x3) << 3) + (code[0] & 0xE0) >> 1);
                decode[2] = (byte)((code[1] & 0x7C) >> 2);
            } else { //ISO-639-1
                decode = new byte[2];
                decode[0] = code[0];
                decode[1] = code[1];
            }
            language = System.Text.Encoding.ASCII.GetString(decode);
            decode = new byte[2];
            if (code[3] > 0x80) {
                decode[0] = (byte)(code[2] & 0x1F);
                decode[1] = (byte)(((code[3] & 0x3) << 3) + (code[2] & 0xE0) >> 1);
            } else {
                decode[0] = code[0];
                decode[1] = code[1];
            }
            country = System.Text.Encoding.ASCII.GetString(decode);
            return new CultureInfo(country + "-" + language);
        }

        // for handling loop reference
        private Stack<UInt32> searchstack = new Stack<UInt32>();
        /// <summary>
        /// Find the requested resource, according to config setting, if the config was set.
        /// This method is NOT HANDLING ANY ERROR, yet!!!!
        /// </summary>
        /// <param name="id">resourceID</param>
        /// <returns>the resource, in string format, if resource id not found, return null value</returns>
        private ApkResource QuickSearchResource(UInt32 id) {
            searchstack.Push(id);
            ApkResource res = new ApkResource(id);

            using (MemoryStream ms = new MemoryStream(resources))
            using (BinaryReader br = new BinaryReader(ms)) {
                // FIX: 旧实现硬编码 ms.Seek(8) 跳过 ResTable 头。
                // 实测存在 ResTable_header.headerSize = 20 的变体（加固/重打包写出额外的保留区，
                // 如平顶山银行 1E3F7B.apk：头部为 "02 00 14 00 ..."，字符串池从偏移 20 开始）。
                // 硬编码 8 会让后续所有 package/type/entry 读取整体错位，导致
                // android:icon / android:label 这类 TYPE_REFERENCE 全部解析为空。
                br.ReadInt16();                       // ResTable.type (=0x0002)
                short rootHeaderSize = br.ReadInt16();// ResTable.headerSize（标准 12，加固变体 20）
                br.ReadInt32();                       // ResTable.size
                int packageCount = br.ReadInt32();
                ms.Seek(rootHeaderSize, SeekOrigin.Begin);   // 用真实头部长度定位全局字符串池

                // 全局字符串池：先做基本校验，异常结构直接放弃，避免读出垃圾偏移
                ms.Seek(4, SeekOrigin.Current);
                int stringPoolSize = br.ReadInt32();
                if (stringPoolSize < 8 || rootHeaderSize + stringPoolSize > resources.Length) {
                    Log("malformed resources.arsc: headerSize=" + rootHeaderSize + " stringPoolSize=" + stringPoolSize);
                    searchstack.Pop();
                    return res;
                }
                ms.Seek(stringPoolSize - 8, SeekOrigin.Current); // jump to the end

                //Package chunk now
                for (int pack = 0; pack < packageCount; pack++) {
                    if (ms.Position + 8 > resources.Length) break;
                    long PackChunkPos = ms.Position;
                    ms.Seek(2, SeekOrigin.Current); // jump type/headersize
                    int headerSize = br.ReadInt16();
                    int PackChunkSize = br.ReadInt32();
                    if (PackChunkSize <= 0 || PackChunkPos + PackChunkSize > resources.Length) break;
                    int packID = br.ReadInt32();

                    if (packID != res.PackageID) { // check if the resource is in this pack
                                                   // goto next chunk
                        ms.Seek(PackChunkPos + PackChunkSize, SeekOrigin.Begin);
                        continue;
                    } else {
                        //ms.Seek(128*2, SeekOrigin.Current); // skip name
                        //int typeStringsPos = br.ReadInt32();
                        //ms.Seek(4,SeekOrigin.Current);    // skip lastpublictype
                        //int keyStringsPos = br.ReadInt32();
                        //ms.Seek(4, SeekOrigin.Current);  // skip lastpublickey
                        ms.Seek(PackChunkPos + headerSize, SeekOrigin.Begin);

                        // skip typestring chunk
                        ms.Seek(4, SeekOrigin.Current);
                        ms.Seek(br.ReadInt32() - 8, SeekOrigin.Current); // jump to the end
                                                                         // skip keystring chunk
                        ms.Seek(4, SeekOrigin.Current);
                        ms.Seek(br.ReadInt32() - 8, SeekOrigin.Current); // jump to the end

                        // come to typespec chunks and type chunks
                        // typespec and type chunks may happen in a row.
                        do {
                            long chunkPos = ms.Position;
                            short chunkType = br.ReadInt16();
                            headerSize = br.ReadInt16();
                            int chunkSize = br.ReadInt32();
                            byte typeid;

                            // FIX: chunkSize 非法时直接结束，避免死循环/越界读取
                            if (chunkSize <= 0 || chunkPos + chunkSize > PackChunkPos + PackChunkSize) {
                                break;
                            }

                            if (chunkType == (short)RES_TYPE.RES_TABLE_TYPE_TYPE) {
                                typeid = br.ReadByte();
                                if (typeid == res.TypeID) {
                                    byte typeFlags = br.ReadByte();
                                    ms.Seek(2, SeekOrigin.Current); // skip reserved
                                    int entryCount = br.ReadInt32();
                                    int entryStart = br.ReadInt32();

                                    // read the config section
                                    int config_size = br.ReadInt32();
                                    byte[] conf = br.ReadBytes(config_size - 4);

                                    // FIX: ResTable_type 支持 FLAG_SPARSE(0x01) / FLAG_OFFSET16(0x02)，
                                    // 旧实现一律按 32 位偏移数组读，遇到这两种表会读到垃圾偏移，
                                    // 于是要么找不到条目(0x7F0F0006)，要么读出乱码字符串(ۤۦ)。
                                    long idxBase = chunkPos + headerSize;
                                    bool sparse = (typeFlags & 0x01) != 0;
                                    bool offset16 = (typeFlags & 0x02) != 0;
                                    uint entryIndic = 0xffffffff;

                                    if (sparse) {
                                        for (int s = 0; s < entryCount; s++) {
                                            ms.Seek(idxBase + s * 4, SeekOrigin.Begin);
                                            ushort sidx = br.ReadUInt16();
                                            ushort soff = br.ReadUInt16();
                                            if ((uint)sidx == res.EntryID) {
                                                entryIndic = (uint)soff * 4;
                                                break;
                                            }
                                        }
                                    } else if (offset16) {
                                        if (res.EntryID < 0x10000) {
                                            ms.Seek(idxBase + (long)res.EntryID * 2, SeekOrigin.Begin);
                                            if (ms.Position + 2 <= chunkPos + chunkSize) {
                                                ushort o16 = br.ReadUInt16();
                                                entryIndic = (o16 == 0xffff) ? 0xffffffff : (uint)o16 * 4;
                                            }
                                        }
                                    } else {
                                        ms.Seek(idxBase + res.EntryID * 4, SeekOrigin.Begin);
                                        entryIndic = br.ReadUInt32();
                                    }

                                    if (entryIndic == 0xffffffff) {
                                        ms.Seek(chunkPos + chunkSize, SeekOrigin.Begin);
                                        continue; //no entry here, go to next chunk
                                    }
                                    ms.Seek(chunkPos + entryStart + entryIndic, SeekOrigin.Begin);

                                    // get to the entry
                                    ms.Seek(11, SeekOrigin.Current); // skip entry size, flags, key, size, 0
                                    byte dataType = br.ReadByte();
                                    uint data = br.ReadUInt32();
                                    if (dataType == (byte)DATA_TYPE.TYPE_STRING) {
                                        res.Add(conf, QuickSearchStringPool(resources, data));
                                    } else if (dataType == (byte)DATA_TYPE.TYPE_REFERENCE) {
                                        // the entry is null, or it's referencing in loop, go to next chunk
                                        if (data == 0x00000000 || searchstack.Contains(data)) {
                                            ms.Seek(chunkPos + chunkSize, SeekOrigin.Begin);
                                            continue;
                                        }
                                        res.Add(QuickSearchResource((UInt32)data));
                                    } else { // I would like to expect we only will recieve TYPE_STRING/TYPE_REFERENCE/any integer type, complex is not considering here,yet
                                        res.Add(conf, data.ToString());
                                    }
                                }
                            }
                            ms.Seek(chunkPos + chunkSize, SeekOrigin.Begin); // skip this chunk
                        } while (ms.Position < PackChunkPos + PackChunkSize);
                        // 安全对齐到包尾，避免异常 chunkSize 造成后续包解析漂移
                        ms.Seek(PackChunkPos + PackChunkSize, SeekOrigin.Begin);
                    }
                }
                searchstack.Pop();
                return res;
            }
        }

        private int applyFilter(ApkResource r) {
            int best = 0;
            if (getFlag("Density") != null) {
                int bestDensity = 0;
                bool supAI = Utility.GetSetting("SupportAdaptiveIcon", "False") == "True";
                for (int i = 0; i < r.configs.Count; i++) {
                    if (!supAI) {
                        // FIX: 旧实现遇到第一个 .xml 就 break，扫描被提前截断，
                        // 结果 best 仍停在 config[0]（通常是最低密度 mdpi/ldpi），
                        // 于是「有 xxxhdpi 192x192，却选了 mdpi 48x48」。
                        // 正确做法是跳过 XML 继续扫描。
                        object val = (i < r.values.Count) ? r.values[i] : null;
                        if (val != null && val.ToString().EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    byte[] bytes = r.configs[i];
                    if (bytes == null || bytes.Length <= (int)ResourceConfig.Density + 1) continue;
                    int density = bytes[(int)ResourceConfig.Density + 1] * 256 + bytes[(int)ResourceConfig.Density];
                    if (density > bestDensity) {
                        bestDensity = density;
                        best = i;
                    }
                }
            }
            return best;
        }

        #region IDispose
        private bool disposed = false;
        protected override void Dispose(bool disposing) {
            if (disposed) return;
            if (disposing) {
                resources = null;
                manifest = null;
                if (zip != null)
                    zip.Close();
            }
            disposed = true;
            base.Dispose(disposing);
        }

        public void Close() {
            Dispose(true);
        }

        ~ApkReader() {
            Dispose(true);
        }
        #endregion
    }

    public class ApkResource
    {
        protected UInt32 ID;
        public List<byte[]> configs;
        public List<object> values;

        public ApkResource(UInt32 id) {
            ID = id;
            configs = new List<byte[]>();
            values = new List<object>();
        }

        public uint PackageID {
            get {
                return (ID & 0xff000000) >> 24;
            }
        }
        public uint TypeID {
            get {
                return (ID & 0x00ff0000) >> 16;
            }
        }
        public uint EntryID {
            get {
                return (ID & 0x0000ffff);
            }
        }
        public object defaultValue {
            set {
                if (configs.Count == 0)
                    configs.Add(new byte[0]);
                else
                    configs[0] = new byte[0];
                if (values.Count == 0)
                    values.Add(value);
                else
                    values[0] = value;
            }
            get {
                if (Count >= 1)
                    return values[0];
                else
                    return null;
            }
        }

        public uint Count {
            get {
                return (uint)configs.Count;
            }
        }

        public void Add(byte[] conf, object val) {
            configs.Add(conf);
            values.Add(val);
        }

        public void Add(object val) {
            configs.Add(new byte[0]);
            values.Add(val);
        }

        public void Add(ApkResource res) {
            for (int i = 0; i < res.Count; i++) {
                Add(res.configs[i], res.values[i]);
            }
        }
    }
}
